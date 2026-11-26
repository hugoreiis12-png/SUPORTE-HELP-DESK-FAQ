using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace Comprove.HelpDesk.Api.SignalR;

// Limitação conhecida: o grupo é por tenant, então todos os usuários do tenant recebem o aviso (só id e status).
// Refinar para grupo por usuário/equipe quando a regra de visibilidade estiver fechada (D-10).
public sealed class SignalRTicketNotifier(IHubContext<TicketsHub> hub) : ITicketNotifier
{
    private const string TicketChangedEvent = "ticketChanged";

    public Task NotifyAsync(TenantId tenant, string ticketId, string status, CancellationToken ct) =>
        hub.Clients.Group(TicketGroups.ForTenant(tenant.Value)).SendAsync(TicketChangedEvent, new { ticketId, status }, ct);
}
