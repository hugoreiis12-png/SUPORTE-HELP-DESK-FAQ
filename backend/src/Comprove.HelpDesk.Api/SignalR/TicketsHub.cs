using Comprove.HelpDesk.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Comprove.HelpDesk.Api.SignalR;

// SignalR só avisa; o cliente refaz o GET para ler o estado autoritativo (B21).
[Authorize]
public sealed class TicketsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenant = Context.User?.FindFirst(RequestContextFactory.TenantClaim)?.Value;
        if (string.IsNullOrWhiteSpace(tenant))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, TicketGroups.ForTenant(tenant));
        await base.OnConnectedAsync();
    }
}

public static class TicketGroups
{
    public static string ForTenant(string tenantId) => $"tenant:{tenantId}";
}
