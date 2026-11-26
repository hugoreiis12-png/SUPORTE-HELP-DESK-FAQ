using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Abstractions;

// Toda operação recebe o tenant: não existe acesso a ticket sem filtro tenant_id.
public interface ITicketRepository
{
    Task<Ticket?> FindAsync(TenantId tenant, string ticketId, CancellationToken ct);

    Task<Ticket?> FindByIdempotencyKeyAsync(TenantId tenant, string idempotencyKey, CancellationToken ct);

    // Protocolo repetido lança ProtocolCollisionException; idempotency_key repetida, ConcurrencyConflictException.
    Task InsertAsync(Ticket ticket, CancellationToken ct);

    // Lança ConcurrencyConflictException se Version no banco ≠ expectedVersion.
    Task UpdateAsync(Ticket ticket, long expectedVersion, CancellationToken ct);

    Task<TicketPage> ListAsync(TicketQuery query, CancellationToken ct);
}
