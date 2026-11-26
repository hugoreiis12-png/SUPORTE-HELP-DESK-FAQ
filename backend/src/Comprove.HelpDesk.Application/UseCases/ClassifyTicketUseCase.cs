using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Application.UseCases;

// Worker de classificação (B16): Recebido → EmTriagem com severidade e SLA.
public sealed class ClassifyTicketUseCase(
    ITicketRepository tickets,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ISlaPolicyProvider slaPolicies,
    ITransactionRunner transactions,
    IClock clock)
{
    public async Task ExecuteAsync(RequestContext context, string ticketId, CancellationToken ct)
    {
        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        var expectedVersion = ticket.Snapshot().Version;
        var now = clock.UtcNow;
        var sla = await slaPolicies.GetActiveAsync(ct);
        var change = ticket.Classify(sla, now);

        await transactions.RunAsync(async token =>
        {
            await tickets.UpdateAsync(ticket, expectedVersion, token);
            await history.AppendAsync(AuditFactory.Create(ticket, TicketChangeType.Classified, context.ActorId, now, context.CorrelationId, change), token);
            await outbox.AppendAsync(TicketOutbox.StatusChanged(ticket, change, context, now), token);
        }, ct);
    }
}
