using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.UseCases;

public sealed record OpenTicketCommand(
    string Title,
    string Description,
    string CategoryId,
    ImpactLevel Impact,
    ScopeLevel Scope,
    FrequencyLevel Frequency,
    UrgencyLevel Urgency,
    string? IdempotencyKey);

// Ticket + histórico + outbox na mesma transação (B10). Retorna o protocolo antes de qualquer Worker.
public sealed class OpenTicketUseCase(
    ITicketRepository tickets,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ITransactionRunner transactions,
    ProtocolGenerator protocols,
    IClock clock,
    ITicketNotifier notifier)
{
    private const int MaxProtocolAttempts = 3;

    public async Task<Ticket> ExecuteAsync(RequestContext context, OpenTicketCommand command, CancellationToken ct)
    {
        if (command.IdempotencyKey is { } key
            && await tickets.FindByIdempotencyKeyAsync(context.TenantId, key, ct) is { } existing)
        {
            return existing;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await OpenOnceAsync(context, command, ct);
            }
            catch (ProtocolCollisionException) when (attempt < MaxProtocolAttempts)
            {
            }
        }
    }

    // Nome, celular e papéis entram no chamado na mesma transação que publica o evento de abertura.
    private static RequesterProfile ToProfile(RequestContext context) =>
        new(context.ActorName, PhoneNumber.Normalize(context.ActorPhone), context.Roles.ToArray());

    private async Task<Ticket> OpenOnceAsync(RequestContext context, OpenTicketCommand command, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var protocol = await protocols.NextAsync(now.Year, ct);
        var ticket = Ticket.Open(
            Guid.NewGuid().ToString("N"),
            context.TenantId,
            protocol,
            context.ActorId,
            command.CategoryId,
            command.Title,
            command.Description,
            command.Impact,
            command.Scope,
            command.Frequency,
            command.Urgency,
            now,
            command.IdempotencyKey,
            ToProfile(context));

        await transactions.RunAsync(async token =>
        {
            await tickets.InsertAsync(ticket, token);
            await history.AppendAsync(AuditFactory.Create(ticket, TicketChangeType.Created, context.ActorId, now, context.CorrelationId, idempotencyKey: command.IdempotencyKey), token);
            await outbox.AppendAsync(TicketOutbox.Created(ticket, context, now), token);
        }, ct);

        var state = ticket.Snapshot();
        await notifier.NotifyAsync(context.TenantId, state.Id, state.Status.ToString(), ct);
        return ticket;
    }
}
