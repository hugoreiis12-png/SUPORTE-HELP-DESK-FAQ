using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Abstractions;

public interface IAttachmentRepository
{
    Task InsertAsync(TicketAttachment attachment, CancellationToken ct);

    // Só depois de validar a assinatura do link: a chave assinada já identifica tenant, chamado e anexo.
    Task<TicketAttachment?> FindByObjectKeyAsync(string objectKey, CancellationToken ct);

    // Em lote: uma consulta para vários chamados (sem N+1).
    Task<IReadOnlyList<TicketAttachment>> ListByTicketIdsAsync(TenantId tenant, IReadOnlyCollection<string> ticketIds, CancellationToken ct);
}
