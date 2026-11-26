using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Infrastructure.Services;

// Usado pelo Worker (sem clientes conectados). A API registra o notificador SignalR por cima.
public sealed class NoOpTicketNotifier : ITicketNotifier
{
    public Task NotifyAsync(TenantId tenant, string ticketId, string status, CancellationToken ct) => Task.CompletedTask;
}
