using System.Net;
using System.Net.Http.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.ValueObjects;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Api;

// Requer MongoDB em replica set. Classificação e despacho rodam direto pelos casos de uso, sem RabbitMQ.
public sealed class SupportFlowTests(HelpDeskApiFactory factory) : IClassFixture<HelpDeskApiFactory>
{
    private const string DefaultQueueId = "suporte-geral";
    private readonly string _tenant = $"tenant-{Guid.NewGuid():N}";

    private HttpClient Requester => factory.CreateClientFor(_tenant, "requester");

    private HttpClient Agent => factory.CreateClientFor(_tenant, "agent", RequestContext.SupportAgentRole);

    private static CreateFaqRequest FaqRequest(string key) => new(
        "Pedido some", "O pedido some ao sincronizar", "Logística & Rotas", "Tela de rotas",
        "Falha de Sincronização", "Pedido duplicado", "Medio", "Medio", "Media", "Media", key);

    private async Task<FaqResponse> CreateFaqAsync(string? key = null)
    {
        var response = await Requester.PostAsJsonAsync("/api/v1/faqs", FaqRequest(key ?? Guid.NewGuid().ToString("N")));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FaqResponse>())!;
    }

    private async Task RunWorkerAsync(string ticketId)
    {
        var context = new RequestContext(TenantId.From(_tenant), RequestContext.SystemActor, [], "test");
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ClassifyTicketUseCase>().ExecuteAsync(context, ticketId, default);
        await scope.ServiceProvider.GetRequiredService<DispatchTicketUseCase>().ExecuteAsync(context, ticketId, default);
    }

    private Task<SupportTicketResponse?> GetTicketAsync(string ticketId) =>
        Agent.GetFromJsonAsync<SupportTicketResponse>($"/api/v1/support/tickets/{ticketId}");

    private Task<HttpResponseMessage> PublishAsync(string ticketId, string key, string status = "Resolvido") =>
        Agent.PostAsJsonAsync($"/api/v1/support/tickets/{ticketId}/resolution", new PublishResolutionRequest("Reinicie o app", status, key));

    private async Task SetDefaultTeamAsync(params string[] agents)
    {
        var mongo = factory.Services.GetRequiredService<MongoContext>();
        await mongo.Collection<SupportTeamDocument>(MongoCollectionNames.SupportTeams).ReplaceOneAsync(
            team => team.QueueId == DefaultQueueId,
            new SupportTeamDocument { QueueId = DefaultQueueId, Agents = [.. agents] },
            new ReplaceOptions { IsUpsert = true });
    }

    [Fact]
    public async Task Repeating_the_faq_creation_with_the_same_key_returns_the_same_pair()
    {
        var key = Guid.NewGuid().ToString("N");

        var first = await CreateFaqAsync(key);
        var second = await CreateFaqAsync(key);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.TicketId, second.TicketId);
    }

    [Fact]
    public async Task Category_without_queue_falls_back_to_the_default_queue_and_can_be_resolved()
    {
        await SetDefaultTeamAsync();
        var faq = await CreateFaqAsync();

        await RunWorkerAsync(faq.TicketId);
        var response = await PublishAsync(faq.TicketId, Guid.NewGuid().ToString("N"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<SupportTicketResponse>();
        Assert.Equal("Resolvido", ticket!.Status);
        Assert.Equal("Resolvido", ticket.FaqTargetStatus);
    }

    [Fact]
    public async Task Dispatch_to_an_agent_reflects_the_status_on_the_faq()
    {
        await SetDefaultTeamAsync("agent");
        var faq = await CreateFaqAsync();

        await RunWorkerAsync(faq.TicketId);

        var ticket = await GetTicketAsync(faq.TicketId);
        Assert.Equal("Em Atendimento", ticket!.Status);
        Assert.Equal("Em Análise", ticket.FaqTargetStatus);
        Assert.Equal("agent", ticket.AssignedAgent);
    }

    [Theory]
    [InlineData("Resolvido")]
    [InlineData("Solução de Contorno")]
    public async Task Repeating_the_resolution_with_the_same_key_does_not_duplicate_the_message(string status)
    {
        await SetDefaultTeamAsync();
        var faq = await CreateFaqAsync();
        await RunWorkerAsync(faq.TicketId);
        var key = Guid.NewGuid().ToString("N");

        var first = await PublishAsync(faq.TicketId, key, status);
        var second = await PublishAsync(faq.TicketId, key, status);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var ticket = await GetTicketAsync(faq.TicketId);
        Assert.Equal(status, ticket!.FaqTargetStatus);
        Assert.Single(ticket.Messages, message => message.Message.Contains("Reinicie o app"));
    }

    [Fact]
    public async Task Simultaneous_resolutions_with_the_same_key_publish_only_once()
    {
        await SetDefaultTeamAsync();
        var faq = await CreateFaqAsync();
        await RunWorkerAsync(faq.TicketId);
        var key = Guid.NewGuid().ToString("N");

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => PublishAsync(faq.TicketId, key, "Solução de Contorno")));

        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
        var ticket = await GetTicketAsync(faq.TicketId);
        Assert.Single(ticket!.Messages, message => message.Message.Contains("Reinicie o app"));
    }

    [Fact]
    public async Task Rejected_resolution_leaves_ticket_and_faq_untouched()
    {
        var faq = await CreateFaqAsync();

        var response = await PublishAsync(faq.TicketId, Guid.NewGuid().ToString("N"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var ticket = await GetTicketAsync(faq.TicketId);
        Assert.Equal("Novo", ticket!.Status);
        Assert.Equal("Pendente", ticket.FaqTargetStatus);
        Assert.Single(ticket.Messages);
    }

    [Fact]
    public async Task Resolution_is_reserved_to_support_agents()
    {
        var faq = await CreateFaqAsync();

        var response = await Requester.PostAsJsonAsync(
            $"/api/v1/support/tickets/{faq.TicketId}/resolution", new PublishResolutionRequest("x", "Resolvido", "k"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
