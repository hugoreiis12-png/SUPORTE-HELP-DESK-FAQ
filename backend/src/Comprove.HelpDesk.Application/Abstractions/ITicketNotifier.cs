using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Abstractions;

// Aviso em tempo real após commit. Cliente deve refazer o fetch para ler o estado autoritativo (B21).
public interface ITicketNotifier
{
    Task NotifyAsync(TenantId tenant, string ticketId, string status, CancellationToken ct);
}
