using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.Rules;

namespace Comprove.HelpDesk.Application.UseCases;

public sealed record PublishResolutionCommand(
    string TicketId,
    string Summary,
    FaqResolutionStatus FaqStatus,
    string IdempotencyKey);

// "Resolver e sincronizar": chamado, mensagem, histórico, outbox e FAQ numa única transação, idempotente pela chave.
public sealed class PublishResolutionUseCase(
    ITicketRepository tickets,
    IFaqRepository faqs,
    ITicketMessageRepository messages,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ITransactionRunner transactions,
    IClock clock,
    ITicketNotifier notifier)
{
    private const string ReopenReason = "Resolução oficial revisada pelo suporte.";

    public async Task<Faq> ExecuteAsync(RequestContext context, PublishResolutionCommand command, CancellationToken ct)
    {
        TicketAccess.RequireAgent(context);
        EnsureValid(command);
        var ticket = await tickets.FindAsync(context.TenantId, command.TicketId, ct)
            ?? throw new TicketNotFoundException(command.TicketId);
        var faq = await faqs.FindByTicketIdAsync(context.TenantId, command.TicketId, ct)
            ?? throw new FaqNotFoundException(command.TicketId);
        if (await history.ExistsWithIdempotencyKeyAsync(context.TenantId, command.TicketId, command.IdempotencyKey, ct))
        {
            return faq;
        }

        var published = await PublishAsync(context, ticket, faq, command, ct);
        await notifier.NotifyAsync(context.TenantId, command.TicketId, ticket.Snapshot().Status.ToString(), ct);
        return published;
    }

    private async Task<Faq> PublishAsync(RequestContext context, Ticket ticket, Faq faq, PublishResolutionCommand command, CancellationToken ct)
    {
        var expectedVersion = ticket.Snapshot().Version;
        var now = clock.UtcNow;
        ticket.RegisterActivity(now);
        var changes = MoveTicket(ticket, command.FaqStatus, now);
        var published = faq.WithResolution(command.FaqStatus, command.Summary, now);

        await transactions.RunAsync(async token =>
        {
            await tickets.UpdateAsync(ticket, expectedVersion, token);
            await AppendChangesAsync(context, ticket, changes, now, token);
            await AppendMessageAsync(context, ticket, command, now, token);
            await faqs.UpdateResolutionAsync(published, token);
        }, ct);

        return published;
    }

    private static void EnsureValid(PublishResolutionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            throw new DomainRuleViolationException("idempotency_key obrigatória para publicar a resolução.");
        }

        if (string.IsNullOrWhiteSpace(command.Summary) || command.Summary.Trim().Length > Faq.MaxResolutionLength)
        {
            throw new DomainRuleViolationException($"Resolução obrigatória com até {Faq.MaxResolutionLength} caracteres.");
        }
    }

    private static List<StatusChange> MoveTicket(Ticket ticket, FaqResolutionStatus target, DateTimeOffset now)
    {
        var wantsResolved = target == FaqResolutionStatus.Resolvido;
        var current = ticket.Snapshot().Status;
        var changes = new List<StatusChange>();
        if (current is TicketStatus.AguardandoAtendimento or TicketStatus.AguardandoSolicitante)
        {
            changes.Add(ticket.Transition(TicketStatus.EmAtendimento, now, null));
        }
        else if (current == TicketStatus.Resolvido && !wantsResolved)
        {
            changes.Add(ticket.Transition(TicketStatus.EmAtendimento, now, ReopenReason));
        }

        if (wantsResolved && current != TicketStatus.Resolvido)
        {
            changes.Add(ticket.Transition(TicketStatus.Resolvido, now, null));
        }

        return changes;
    }

    private async Task AppendChangesAsync(
        RequestContext context,
        Ticket ticket,
        IReadOnlyList<StatusChange> changes,
        DateTimeOffset now,
        CancellationToken ct)
    {
        foreach (var change in changes)
        {
            await history.AppendAsync(AuditFactory.Create(ticket, ChangeTypeOf(change), context.ActorId, now, context.CorrelationId, change), ct);
            var message = change.To == TicketStatus.Resolvido
                ? TicketOutbox.Resolved(ticket, context, now)
                : TicketOutbox.StatusChanged(ticket, change, context, now);
            await outbox.AppendAsync(message, ct);
        }
    }

    private static TicketChangeType ChangeTypeOf(StatusChange change)
    {
        if (change.To == TicketStatus.Resolvido)
        {
            return TicketChangeType.Resolved;
        }

        return TicketStateMachine.IsReopen(change.From, change.To) ? TicketChangeType.Reopened : TicketChangeType.StatusChanged;
    }

    // A chave fica no histórico desta publicação: repetir a chamada encontra o registro e não duplica.
    private async Task AppendMessageAsync(
        RequestContext context,
        Ticket ticket,
        PublishResolutionCommand command,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var message = new TicketMessage(
            Guid.NewGuid().ToString("N"),
            command.TicketId,
            context.TenantId,
            context.ActorId,
            $"Resolução oficial ({command.FaqStatus}) publicada no FAQ: {command.Summary.Trim()}",
            MessageVisibility.Public,
            now,
            context.ActorName ?? context.ActorId,
            MessageAuthorRoles.Support);
        await messages.AppendAsync(message, ct);
        var audit = AuditFactory.Create(
            ticket, TicketChangeType.MessageAdded, context.ActorId, now, context.CorrelationId, idempotencyKey: command.IdempotencyKey);
        await history.AppendAsync(audit, ct);
        await outbox.AppendAsync(TicketOutbox.CommentCreated(ticket, message.Id, context, now), ct);
    }
}
