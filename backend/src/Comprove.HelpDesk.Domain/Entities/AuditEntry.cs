using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Domain.Entities;

// Linha de ticket_history (B06). Gravada na mesma transação da mudança.
public sealed record AuditEntry(
    string Id,
    string TicketId,
    TenantId TenantId,
    TicketChangeType ChangeType,
    TicketStatus? FromStatus,
    TicketStatus? ToStatus,
    string Actor,
    DateTimeOffset OccurredAt,
    string? Reason,
    string CorrelationId,
    string? IdempotencyKey);
