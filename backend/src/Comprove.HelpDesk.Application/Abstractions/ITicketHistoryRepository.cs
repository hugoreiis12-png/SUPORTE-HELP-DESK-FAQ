using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Abstractions;

public interface ITicketHistoryRepository
{
    Task AppendAsync(AuditEntry entry, CancellationToken ct);

    Task<bool> ExistsWithIdempotencyKeyAsync(TenantId tenant, string ticketId, string idempotencyKey, CancellationToken ct);
}
