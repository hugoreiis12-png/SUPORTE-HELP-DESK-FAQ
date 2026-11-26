using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Abstractions;

public interface ITicketMessageRepository
{
    Task AppendAsync(TicketMessage message, CancellationToken ct);

    Task<IReadOnlyList<TicketMessage>> ListPublicAsync(TenantId tenant, string ticketId, CancellationToken ct);

    // Visão do agente: inclui notas internas. Em lote para a listagem do Help Desk.
    Task<IReadOnlyList<TicketMessage>> ListAllByTicketIdsAsync(TenantId tenant, IReadOnlyCollection<string> ticketIds, CancellationToken ct);
}
