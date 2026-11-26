using System.Net;
using System.Net.Http.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.ValueObjects;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Api;

// As quatro respostas são gravadas e o Worker classifica o chamado a partir delas (ADR-012).
public sealed class TriageScoringTests(HelpDeskApiFactory factory) : IClassFixture<HelpDeskApiFactory>
{
    private readonly string _tenant = $"tenant-{Guid.NewGuid():N}";

    private HttpClient Agent => factory.CreateClientFor(_tenant, "agent", RequestContext.SupportAgentRole);

    private static CreateFaqRequest Request(string scope, string urgency) => new(
        "Pedido some", "O pedido some ao sincronizar", "Logística & Rotas", "Tela de rotas",
        "Falha de Sincronização", "Pedido duplicado", "Medio", scope, "Alta", urgency, Guid.NewGuid().ToString("N"));

    private async Task<FaqResponse> CreateFaqAsync(string scope, string urgency)
    {
        var response = await Agent.PostAsJsonAsync("/api/v1/faqs", Request(scope, urgency));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FaqResponse>())!;
    }

    private async Task<SupportTicketResponse> ClassifyAndReadAsync(string ticketId)
    {
        var context = new RequestContext(TenantId.From(_tenant), RequestContext.SystemActor, [], "test");
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ClassifyTicketUseCase>().ExecuteAsync(context, ticketId, default);
        return (await Agent.GetFromJsonAsync<SupportTicketResponse>($"/api/v1/support/tickets/{ticketId}"))!;
    }

    [Fact]
    public async Task Worker_classifies_the_ticket_from_the_four_answers()
    {
        var faq = await CreateFaqAsync("Alto", "Alta");

        var ticket = await ClassifyAndReadAsync(faq.TicketId);

        Assert.Equal("Crítica", ticket.Severity);
        Assert.Equal(90, ticket.SeverityScore);
        Assert.Equal("Alta", ticket.SeverityBreakdown!.Urgency);
        Assert.Equal(TimeSpan.FromHours(2), ticket.SlaDeadline!.Value - ticket.CreatedAt);
    }

    [Fact]
    public async Task Records_saved_before_the_urgency_question_can_still_be_read_and_classified()
    {
        var faq = await CreateFaqAsync("Medio", "Alta");
        var mongo = factory.Services.GetRequiredService<MongoContext>();
        await mongo.Database.GetCollection<BsonDocument>(MongoCollectionNames.Tickets).UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", faq.TicketId),
            Builders<BsonDocument>.Update.Unset("urgency"));

        var ticket = await ClassifyAndReadAsync(faq.TicketId);

        Assert.Equal("Alta", ticket.Severity);
    }

    [Theory]
    [InlineData("999")]
    [InlineData("25")]
    [InlineData("Urgentissima")]
    [InlineData("")]
    public async Task Rejects_answers_that_are_not_a_known_level(string urgency)
    {
        var response = await Agent.PostAsJsonAsync("/api/v1/faqs", Request("Medio", urgency));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
