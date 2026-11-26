using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Services;

// Resolve em lote a imagem de cada chamado (só upload concluído) e assina um link curto de download.
public sealed class AttachmentLinkResolver(IAttachmentRepository attachments, IObjectStorage storage, AttachmentLimits limits)
{
    public async Task<IReadOnlyDictionary<string, AttachmentLink>> ForTicketsAsync(
        TenantId tenant,
        IReadOnlyCollection<string> ticketIds,
        CancellationToken ct)
    {
        var links = new Dictionary<string, AttachmentLink>();
        if (ticketIds.Count == 0)
        {
            return links;
        }

        var found = await attachments.ListByTicketIdsAsync(tenant, ticketIds, ct);
        var stored = await storage.FindStoredAsync(found.Select(a => a.ObjectKey).ToArray(), ct);
        var firstImages = found
            .Where(a => AttachmentContentTypes.IsAllowed(a.ContentType) && stored.Contains(a.ObjectKey))
            .OrderBy(a => a.CreatedAt)
            .GroupBy(a => a.TicketId)
            .Select(group => group.First());

        foreach (var image in firstImages)
        {
            var url = await storage.CreateDownloadUrlAsync(image.ObjectKey, limits.UploadUrlTtl, ct);
            links[image.TicketId] = new AttachmentLink(image.Id, image.ContentType, url);
        }

        return links;
    }
}
