using System.Net.Http.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Api;

// Severidade e critérios de triagem são informação do Help Desk: o usuário final não os recebe.
public sealed class SeverityVisibilityTests(HelpDeskApiFactory factory) : IClassFixture<HelpDeskApiFactory>
{
    private readonly string _tenant = $"tenant-{Guid.NewGuid():N}";

    private HttpClient Requester => factory.CreateClientFor(_tenant, "requester");

    private HttpClient Agent => factory.CreateClientFor(_tenant, "agent", RequestContext.SupportAgentRole);

    private static async Task<FaqResponse> CreateFaqAsync(HttpClient client)
    {
        var request = new CreateFaqRequest(
            "Pedido some", "O pedido some ao sincronizar", "Logística & Rotas", "Tela de rotas",
            "Falha de Sincronização", "Pedido duplicado", "Alto", "Alto", "Alta", "Alta", Guid.NewGuid().ToString("N"));
        var response = await client.PostAsJsonAsync("/api/v1/faqs", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FaqResponse>())!;
    }

    private async Task ClassifyAsync(string ticketId)
    {
        var context = new RequestContext(TenantId.From(_tenant), RequestContext.SystemActor, [], "test");
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ClassifyTicketUseCase>().ExecuteAsync(context, ticketId, default);
    }

    private static async Task<FaqResponse[]> ReadFaqsAsync(HttpClient client, string faqId)
    {
        var single = await client.GetFromJsonAsync<FaqResponse>($"/api/v1/faqs/{faqId}");
        var page = await client.GetFromJsonAsync<PagedResponse<FaqResponse>>("/api/v1/faqs");
        return [single!, Assert.Single(page!.Items)];
    }

    private static async Task<TicketResponse[]> ReadTicketsAsync(HttpClient client, string ticketId)
    {
        var single = await client.GetFromJsonAsync<TicketResponse>($"/api/v1/tickets/{ticketId}");
        var page = await client.GetFromJsonAsync<PagedResponse<TicketResponse>>("/api/v1/tickets");
        return [single!, Assert.Single(page!.Items)];
    }

    private static void AssertHidden(FaqResponse faq)
    {
        Assert.Null(faq.Severity);
        Assert.Null(faq.SeverityScore);
        Assert.Null(faq.SeverityBreakdown);
    }

    [Fact]
    public async Task Requester_never_receives_severity_from_faq_routes()
    {
        var created = await CreateFaqAsync(Requester);

        var read = await ReadFaqsAsync(Requester, created.Id);

        AssertHidden(created);
        Assert.All(read, AssertHidden);
    }

    [Fact]
    public async Task Requester_never_receives_triage_data_from_ticket_routes()
    {
        var faq = await CreateFaqAsync(Requester);
        await ClassifyAsync(faq.TicketId);

        var tickets = await ReadTicketsAsync(Requester, faq.TicketId);

        Assert.All(tickets, ticket =>
        {
            Assert.Null(ticket.Severity);
            Assert.Null(ticket.SeverityScore);
            Assert.Null(ticket.Impact);
            Assert.Null(ticket.Scope);
            Assert.Null(ticket.Frequency);
            Assert.Null(ticket.Urgency);
        });
    }

    [Fact]
    public async Task Support_agent_keeps_receiving_severity()
    {
        var faq = await CreateFaqAsync(Agent);
        await ClassifyAsync(faq.TicketId);

        var faqs = await ReadFaqsAsync(Agent, faq.Id);
        var tickets = await ReadTicketsAsync(Agent, faq.TicketId);

        Assert.Equal("Crítica", faq.Severity);
        Assert.All(faqs, item => Assert.Equal("Alto", item.SeverityBreakdown!.Impact));
        Assert.All(tickets, item => Assert.Equal("Crítica", item.Severity));
    }
}
