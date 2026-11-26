using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Abstractions;

// Toda operação recebe o tenant (ADR-006). FAQ e chamado têm relação 1:1 via TicketId.
public interface IFaqRepository
{
    Task<Faq?> FindAsync(TenantId tenant, string faqId, CancellationToken ct);

    Task<Faq?> FindByTicketIdAsync(TenantId tenant, string ticketId, CancellationToken ct);

    Task<IReadOnlyDictionary<string, Faq>> FindByTicketIdsAsync(TenantId tenant, IReadOnlyCollection<string> ticketIds, CancellationToken ct);

    // Contador por tenant; define o código FAQ-NN.
    Task<long> NextNumberAsync(TenantId tenant, CancellationToken ct);

    Task InsertAsync(Faq faq, CancellationToken ct);

    // Grava só status e texto da resolução: votos simultâneos não são sobrescritos.
    Task UpdateResolutionAsync(Faq faq, CancellationToken ct);

    Task UpdateStatusByTicketAsync(TenantId tenant, string ticketId, FaqResolutionStatus status, DateTimeOffset now, CancellationToken ct);

    Task<Page<Faq>> ListAsync(FaqQuery query, CancellationToken ct);

    // Alterna o voto "também tenho essa dúvida" do usuário e devolve o contador resultante.
    Task<FaqUpvote> ToggleUpvoteAsync(TenantId tenant, string faqId, string userId, DateTimeOffset now, CancellationToken ct);

    Task<IReadOnlySet<string>> UpvotedIdsAsync(TenantId tenant, string userId, IReadOnlyCollection<string> faqIds, CancellationToken ct);
}
