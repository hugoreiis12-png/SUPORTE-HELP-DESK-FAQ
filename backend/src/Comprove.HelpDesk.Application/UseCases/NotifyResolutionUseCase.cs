using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Application.UseCases;

public enum ResolutionNoticeOutcome
{
    Sent,
    AlreadySent,
    NotResolved,
    NoPhone,
    NotOnWhatsApp
}

// Avisa quem abriu o chamado. O histórico marca o envio; a chave no gateway cobre a falha entre enviar e marcar.
public sealed class NotifyResolutionUseCase(
    ITicketRepository tickets,
    IFaqRepository faqs,
    ITicketHistoryRepository history,
    IWhatsAppGateway whatsApp,
    IClock clock)
{
    private const string KeyPrefix = "resolution-notice-";

    public async Task<ResolutionNoticeOutcome> ExecuteAsync(
        RequestContext context, string ticketId, string eventId, CancellationToken ct)
    {
        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        var key = KeyPrefix + eventId;
        if (await history.ExistsWithIdempotencyKeyAsync(context.TenantId, ticketId, key, ct))
        {
            return ResolutionNoticeOutcome.AlreadySent;
        }

        // Reaberto antes do envio: avisar "resolvido" seria informação errada.
        var state = ticket.Snapshot();
        if (state.Status is not (TicketStatus.Resolvido or TicketStatus.Fechado))
        {
            return ResolutionNoticeOutcome.NotResolved;
        }

        return await SendAsync(context, ticket, key, ct);
    }

    private async Task<ResolutionNoticeOutcome> SendAsync(RequestContext context, Ticket ticket, string key, CancellationToken ct)
    {
        var state = ticket.Snapshot();
        if (state.Requester?.Phone is not { } phone)
        {
            return ResolutionNoticeOutcome.NoPhone;
        }

        if (await whatsApp.FindChatIdAsync(phone, ct) is not { } chatId)
        {
            return ResolutionNoticeOutcome.NotOnWhatsApp;
        }

        var faq = await faqs.FindByTicketIdAsync(context.TenantId, state.Id, ct);
        await whatsApp.SendTextAsync(new WhatsAppMessage(chatId, ResolutionMessage.Compose(state, faq), key), ct);
        var entry = AuditFactory.Create(
            ticket, TicketChangeType.RequesterNotified, context.ActorId, clock.UtcNow, context.CorrelationId, idempotencyKey: key);
        await history.AppendAsync(entry, ct);
        return ResolutionNoticeOutcome.Sent;
    }
}
