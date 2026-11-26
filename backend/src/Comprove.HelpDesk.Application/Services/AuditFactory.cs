using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Application.Services;

// Única forma de criar linha de ticket_history, para que todo caso de uso grave o mesmo formato.
public static class AuditFactory
{
    public static AuditEntry Create(
        Ticket ticket,
        TicketChangeType changeType,
        string actor,
        DateTimeOffset now,
        string correlationId,
        StatusChange? change = null,
        string? idempotencyKey = null)
    {
        var state = ticket.Snapshot();
        return new AuditEntry(
            Id: Guid.NewGuid().ToString("N"),
            TicketId: state.Id,
            TenantId: state.TenantId,
            ChangeType: changeType,
            FromStatus: change?.From,
            ToStatus: change?.To,
            Actor: actor,
            OccurredAt: now,
            Reason: change?.Reason,
            CorrelationId: correlationId,
            IdempotencyKey: idempotencyKey);
    }
}
