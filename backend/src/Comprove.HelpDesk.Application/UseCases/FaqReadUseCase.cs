using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Application.UseCases;

// Leituras e voto do FAQ. Qualquer usuário autenticado do tenant lê; a listagem é sempre paginada.
public sealed class FaqReadUseCase(IFaqRepository faqs, AttachmentLinkResolver images, IClock clock)
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;

    public async Task<Page<FaqView>> ListAsync(
        RequestContext context,
        FaqResolutionStatus? status,
        string? search,
        DateTimeOffset? createdBefore,
        int? pageSize,
        CancellationToken ct)
    {
        var query = new FaqQuery(
            context.TenantId,
            status,
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            createdBefore,
            Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
        var page = await faqs.ListAsync(query, ct);
        return new Page<FaqView>(await ToViewsAsync(context, page.Items, ct), page.NextCursor);
    }

    public async Task<FaqView> GetAsync(RequestContext context, string faqId, CancellationToken ct)
    {
        var faq = await faqs.FindAsync(context.TenantId, faqId, ct) ?? throw new FaqNotFoundException(faqId);
        return (await ToViewsAsync(context, [faq], ct))[0];
    }

    public async Task<FaqUpvote> ToggleUpvoteAsync(RequestContext context, string faqId, CancellationToken ct)
    {
        _ = await faqs.FindAsync(context.TenantId, faqId, ct) ?? throw new FaqNotFoundException(faqId);
        return await faqs.ToggleUpvoteAsync(context.TenantId, faqId, context.ActorId, clock.UtcNow, ct);
    }

    private async Task<IReadOnlyList<FaqView>> ToViewsAsync(RequestContext context, IReadOnlyList<Faq> items, CancellationToken ct)
    {
        var upvoted = await faqs.UpvotedIdsAsync(context.TenantId, context.ActorId, items.Select(f => f.Id).ToArray(), ct);
        var links = await images.ForTicketsAsync(context.TenantId, items.Select(f => f.TicketId).ToArray(), ct);
        return items
            .Select(f => new FaqView(f, upvoted.Contains(f.Id), links.GetValueOrDefault(f.TicketId)))
            .ToList();
    }
}
