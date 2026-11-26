using System.Net.Http.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Api;

// Do token ao aviso: o celular de quem abriu é gravado no chamado e usado quando o suporte resolve.
public sealed class ResolutionNoticeTests(HelpDeskApiFactory factory) : IClassFixture<HelpDeskApiFactory>
{
    private readonly string _tenant = $"tenant-{Guid.NewGuid():N}";
    private readonly FakeWhatsApp _whatsApp = new();

    private HttpClient Agent => factory.CreateClientFor(_tenant, "agent", RequestContext.SupportAgentRole);

    private RequestContext System => new(TenantId.From(_tenant), RequestContext.SystemActor, [], "test");

    private sealed class FakeWhatsApp : IWhatsAppGateway
    {
        public List<string> CheckedPhones { get; } = [];

        public List<WhatsAppMessage> Sent { get; } = [];

        public string? ChatId { get; set; } = "552199999999@c.us";

        public Task<string?> FindChatIdAsync(string phone, CancellationToken ct)
        {
            CheckedPhones.Add(phone);
            return Task.FromResult(ChatId);
        }

        public Task SendTextAsync(WhatsAppMessage message, CancellationToken ct)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private async Task<string> OpenAsync(string phone)
    {
        var client = factory.CreateClientWithProfile(_tenant, "thiago", "Thiago", phone);
        var request = new CreateFaqRequest(
            "Pedido some", "O pedido some ao sincronizar", "Logística & Rotas", "Tela de rotas",
            "Falha de Sincronização", "Pedido duplicado", "Medio", "Medio", "Media", "Media", Guid.NewGuid().ToString("N"));
        var response = await client.PostAsJsonAsync("/api/v1/faqs", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FaqResponse>())!.TicketId;
    }

    private async Task ResolveAsync(string ticketId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ClassifyTicketUseCase>().ExecuteAsync(System, ticketId, default);
        await scope.ServiceProvider.GetRequiredService<DispatchTicketUseCase>().ExecuteAsync(System, ticketId, default);

        var note = new AddTicketMessageRequest("SEGREDO INTERNO", "Internal");
        (await Agent.PostAsJsonAsync($"/api/v1/tickets/{ticketId}/messages", note)).EnsureSuccessStatusCode();
        var resolution = new PublishResolutionRequest("Atualize o aplicativo", "Resolvido", Guid.NewGuid().ToString("N"));
        (await Agent.PostAsJsonAsync($"/api/v1/support/tickets/{ticketId}/resolution", resolution)).EnsureSuccessStatusCode();
    }

    private async Task<ResolutionNoticeOutcome> NotifyAsync(string ticketId, string eventId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var useCase = ActivatorUtilities.CreateInstance<NotifyResolutionUseCase>(services, _whatsApp);
        return await useCase.ExecuteAsync(System, ticketId, eventId, default);
    }

    [Fact]
    public async Task Sends_the_official_resolution_to_the_phone_of_who_opened_the_ticket()
    {
        var ticketId = await OpenAsync("+55 (21) 99999-9999");
        await ResolveAsync(ticketId);

        var outcome = await NotifyAsync(ticketId, "event-1");

        Assert.Equal(ResolutionNoticeOutcome.Sent, outcome);
        Assert.Equal("5521999999999", Assert.Single(_whatsApp.CheckedPhones));
        var message = Assert.Single(_whatsApp.Sent);
        Assert.Equal("552199999999@c.us", message.ChatId);
        Assert.StartsWith("Olá, Thiago! Seu chamado SUP-", message.Text);
        Assert.Contains("Resolução: Atualize o aplicativo", message.Text);
        Assert.DoesNotContain("SEGREDO INTERNO", message.Text);
    }

    [Fact]
    public async Task Redelivering_the_same_event_does_not_send_twice()
    {
        var ticketId = await OpenAsync("21999999999");
        await ResolveAsync(ticketId);

        await NotifyAsync(ticketId, "event-2");
        var second = await NotifyAsync(ticketId, "event-2");

        Assert.Equal(ResolutionNoticeOutcome.AlreadySent, second);
        Assert.Single(_whatsApp.Sent);
    }

    [Fact]
    public async Task Ticket_without_a_valid_phone_is_not_notified()
    {
        var ticketId = await OpenAsync("sem telefone");
        await ResolveAsync(ticketId);

        Assert.Equal(ResolutionNoticeOutcome.NoPhone, await NotifyAsync(ticketId, "event-3"));
        Assert.Empty(_whatsApp.Sent);
    }

    [Fact]
    public async Task Number_without_whatsapp_is_not_notified()
    {
        _whatsApp.ChatId = null;
        var ticketId = await OpenAsync("21999999999");
        await ResolveAsync(ticketId);

        Assert.Equal(ResolutionNoticeOutcome.NotOnWhatsApp, await NotifyAsync(ticketId, "event-4"));
        Assert.Empty(_whatsApp.Sent);
    }

    [Fact]
    public async Task Open_ticket_is_never_announced_as_resolved()
    {
        var ticketId = await OpenAsync("21999999999");

        Assert.Equal(ResolutionNoticeOutcome.NotResolved, await NotifyAsync(ticketId, "event-5"));
        Assert.Empty(_whatsApp.Sent);
    }

    [Fact]
    public async Task Phone_is_never_returned_by_the_api()
    {
        var ticketId = await OpenAsync("21999999999");

        var support = await Agent.GetStringAsync($"/api/v1/support/tickets/{ticketId}");
        var ticket = await Agent.GetStringAsync($"/api/v1/tickets/{ticketId}");

        Assert.DoesNotContain("21999999999", support);
        Assert.DoesNotContain("21999999999", ticket);
    }
}
