using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Application.UseCases;

// Resolução idempotente por idempotency_key (B18): uma transação, um único evento de resolução.
public sealed class ResolveTicketUseCase(
    ITicketRepository tickets,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ITransactionRunner transactions,
    IClock clock,
    ITicketNotifier notifier,
    IFaqRepository faqs)
{
    public async Task<Ticket> ExecuteAsync(RequestContext context, string ticketId, string idempotencyKey, CancellationToken ct)
    {
        TicketAccess.RequireAgent(context);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new DomainRuleViolationException("idempotency_key obrigatória para resolver.");
        }

        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        if (await history.ExistsWithIdempotencyKeyAsync(context.TenantId, ticketId, idempotencyKey, ct))
        {
            return ticket;
        }

        var expectedVersion = ticket.Snapshot().Version;
        var now = clock.UtcNow;
        var change = ticket.Transition(TicketStatus.Resolvido, now, null);

        await transactions.RunAsync(async token =>
        {
            await tickets.UpdateAsync(ticket, expectedVersion, token);
            await history.AppendAsync(AuditFactory.Create(ticket, TicketChangeType.Resolved, context.ActorId, now, context.CorrelationId, change, idempotencyKey), token);
            await outbox.AppendAsync(TicketOutbox.Resolved(ticket, context, now), token);
            await faqs.UpdateStatusByTicketAsync(context.TenantId, ticketId, FaqResolutionStatus.Resolvido, now, token);
        }, ct);

        await notifier.NotifyAsync(context.TenantId, ticketId, change.To.ToString(), ct);
        return ticket;
    }
}
