using System.Net;
using System.Net.Http.Json;
using Comprove.HelpDesk.Contracts.Http;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Api;

// Requer MongoDB em replica set (rs0). Aponte com ConnectionStrings__HelpDeskMongo.
// Prova ADR-006: sem token → 401; outro tenant → 404 (nunca 403 que vaze existência).
public sealed class TenantIsolationTests(HelpDeskApiFactory factory) : IClassFixture<HelpDeskApiFactory>
{
    [Fact]
    public async Task Request_without_token_is_unauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ticket_of_another_tenant_is_not_found()
    {
        using var clientA = factory.CreateClientFor("tenant-a", "requester-a");
        var open = new OpenTicketRequest("Teste de isolamento", "Descrição", "cat-1", "Alto", "Alto", "Alta", "Alta", Guid.NewGuid().ToString("N"));
        var created = await clientA.PostAsJsonAsync("/api/v1/tickets", open);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>();

        using var clientB = factory.CreateClientFor("tenant-b", "requester-b");
        var response = await clientB.GetAsync($"/api/v1/tickets/{ticket!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Tickets_without_idempotency_key_do_not_collide()
    {
        using var client = factory.CreateClientFor("tenant-a", "requester-a");
        var open = new OpenTicketRequest("Sem chave", "Descrição", "cat-1", "Alto", "Alto", "Alta", "Alta", null);

        var first = await client.PostAsJsonAsync("/api/v1/tickets", open);
        var second = await client.PostAsJsonAsync("/api/v1/tickets", open);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }
}
