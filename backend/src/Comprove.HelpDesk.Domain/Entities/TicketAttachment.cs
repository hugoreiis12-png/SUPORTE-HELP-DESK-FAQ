using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Domain.Entities;

// Metadados apenas. O binário fica no object storage (D-13), nunca no documento do ticket.
public sealed record TicketAttachment(
    string Id,
    string TicketId,
    TenantId TenantId,
    string ObjectKey,
    string ContentType,
    long SizeBytes,
    string CreatedBy,
    DateTimeOffset CreatedAt);
