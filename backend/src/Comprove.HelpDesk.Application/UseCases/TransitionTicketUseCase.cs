using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Rules;

namespace Comprove.HelpDesk.Application.UseCases;

// Transições manuais de agente (B12). Reabertura usa TicketChangeType.Reopened e exige motivo.
public sealed class TransitionTicketUseCase(
    ITicketRepository tickets,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ITransactionRunner transactions,
    IClock clock,
    ITicketNotifier notifier,
    IFaqRepository faqs)
{
    public async Task ExecuteAsync(RequestContext context, string ticketId, TicketStatus target, string? reason, CancellationToken ct)
    {
        TicketAccess.RequireAgent(context);
        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        var expectedVersion = ticket.Snapshot().Version;
        var now = clock.UtcNow;
        var change = ticket.Transition(target, now, reason);
        var changeType = TicketStateMachine.IsReopen(change.From, change.To)
            ? TicketChangeType.Reopened
            : TicketChangeType.StatusChanged;

        await transactions.RunAsync(async token =>
        {
            await tickets.UpdateAsync(ticket, expectedVersion, token);
            await history.AppendAsync(AuditFactory.Create(ticket, changeType, context.ActorId, now, context.CorrelationId, change), token);
            await outbox.AppendAsync(TicketOutbox.StatusChanged(ticket, change, context, now), token);
            await faqs.UpdateStatusByTicketAsync(context.TenantId, ticketId, FaqStatusRules.FromTicket(change.To), now, token);
        }, ct);

        await notifier.NotifyAsync(context.TenantId, ticketId, change.To.ToString(), ct);
    }
}
