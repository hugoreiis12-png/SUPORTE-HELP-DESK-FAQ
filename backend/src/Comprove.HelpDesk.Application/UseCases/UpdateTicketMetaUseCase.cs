using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Application.UseCases;

// PATCH /meta (B20): somente agente altera a categoria.
public sealed class UpdateTicketMetaUseCase(
    ITicketRepository tickets,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ITransactionRunner transactions,
    IClock clock)
{
    public async Task ExecuteAsync(RequestContext context, string ticketId, string categoryId, CancellationToken ct)
    {
        TicketAccess.RequireAgent(context);
        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        var expectedVersion = ticket.Snapshot().Version;
        var now = clock.UtcNow;
        ticket.ChangeCategory(categoryId, now);

        await transactions.RunAsync(async token =>
        {
            await tickets.UpdateAsync(ticket, expectedVersion, token);
            await history.AppendAsync(AuditFactory.Create(ticket, TicketChangeType.MetaUpdated, context.ActorId, now, context.CorrelationId), token);
            await outbox.AppendAsync(TicketOutbox.Updated(ticket, context, now), token);
        }, ct);
    }
}
