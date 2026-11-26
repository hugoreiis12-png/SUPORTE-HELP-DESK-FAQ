using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Rules;

namespace Comprove.HelpDesk.Application.UseCases;

// Worker de dispatch (B17): atribui fila e agente por rodízio auditável.
public sealed class DispatchTicketUseCase(
    ITicketRepository tickets,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ITeamRoster roster,
    IFaqRepository faqs,
    ITransactionRunner transactions,
    IClock clock)
{
    public async Task ExecuteAsync(RequestContext context, string ticketId, CancellationToken ct)
    {
        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        var assignment = await roster.NextAssignmentAsync(ticket.Snapshot().CategoryId, ct);
        var expectedVersion = ticket.Snapshot().Version;
        var now = clock.UtcNow;
        var changes = ticket.Dispatch(assignment.QueueId, assignment.AgentId, now);

        await transactions.RunAsync(async token =>
        {
            await tickets.UpdateAsync(ticket, expectedVersion, token);
            await history.AppendAsync(AuditFactory.Create(ticket, TicketChangeType.Dispatched, RequestContext.SystemActor, now, context.CorrelationId), token);
            foreach (var change in changes)
            {
                await outbox.AppendAsync(TicketOutbox.StatusChanged(ticket, change, context, now), token);
            }

            await faqs.UpdateStatusByTicketAsync(context.TenantId, ticketId, FaqStatusRules.FromTicket(ticket.Snapshot().Status), now, token);
        }, ct);
    }
}
