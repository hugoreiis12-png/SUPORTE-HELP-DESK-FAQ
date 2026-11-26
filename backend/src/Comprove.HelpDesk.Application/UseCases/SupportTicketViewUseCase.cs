using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Application.UseCases;

// Visão do Help Desk (somente agente): chamado + FAQ vinculado + mensagens (com notas internas) + imagem.
public sealed class SupportTicketViewUseCase(
    TicketReadUseCase reads,
    IFaqRepository faqs,
    ITicketMessageRepository messages,
    AttachmentLinkResolver images)
{
    public async Task<Page<SupportTicketView>> ListAsync(
        RequestContext context,
        TicketStatus? status,
        DateTimeOffset? createdBefore,
        int? pageSize,
        CancellationToken ct)
    {
        TicketAccess.RequireAgent(context);
        var page = await reads.ListAsync(context, status, createdBefore, pageSize, ct);
        return new Page<SupportTicketView>(await BuildAsync(context, page.Items, ct), page.NextCursor);
    }

    public async Task<SupportTicketView> GetAsync(RequestContext context, string ticketId, CancellationToken ct)
    {
        TicketAccess.RequireAgent(context);
        var ticket = await reads.GetAsync(context, ticketId, ct);
        return (await BuildAsync(context, [ticket], ct))[0];
    }

    private async Task<IReadOnlyList<SupportTicketView>> BuildAsync(RequestContext context, IReadOnlyList<Ticket> tickets, CancellationToken ct)
    {
        var ids = tickets.Select(t => t.Snapshot().Id).ToArray();
        var faqByTicket = await faqs.FindByTicketIdsAsync(context.TenantId, ids, ct);
        var imageByTicket = await images.ForTicketsAsync(context.TenantId, ids, ct);
        var messagesByTicket = (await messages.ListAllByTicketIdsAsync(context.TenantId, ids, ct))
            .ToLookup(m => m.TicketId);

        return tickets
            .Select(t =>
            {
                var id = t.Snapshot().Id;
                return new SupportTicketView(
                    t,
                    faqByTicket.GetValueOrDefault(id),
                    messagesByTicket[id].OrderBy(m => m.CreatedAt).ToList(),
                    imageByTicket.GetValueOrDefault(id));
            })
            .ToList();
    }
}
