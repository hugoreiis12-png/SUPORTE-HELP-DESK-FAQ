using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Services;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Application.UseCases;

// Mensagens públicas (solicitante ou agente) e notas internas (somente agente). Sem notas internas na listagem (B11).
public sealed class AddTicketMessageUseCase(
    ITicketRepository tickets,
    ITicketMessageRepository messages,
    ITicketHistoryRepository history,
    IOutboxRepository outbox,
    ITransactionRunner transactions,
    IClock clock)
{
    private const int MaxBodyLength = 4000;

    public async Task<TicketMessage> ExecuteAsync(RequestContext context, string ticketId, string body, MessageVisibility visibility, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > MaxBodyLength)
        {
            throw new DomainRuleViolationException($"Mensagem obrigatória com até {MaxBodyLength} caracteres.");
        }

        if (visibility == MessageVisibility.Internal)
        {
            TicketAccess.RequireAgent(context);
        }

        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        TicketAccess.EnsureCanSee(context, ticket);
        ticket.EnsureCanReceiveMessages();

        var now = clock.UtcNow;
        var message = new TicketMessage(
            Guid.NewGuid().ToString("N"),
            ticketId,
            context.TenantId,
            context.ActorId,
            body.Trim(),
            visibility,
            now,
            context.ActorName ?? context.ActorId,
            context.IsAgent ? MessageAuthorRoles.Support : MessageAuthorRoles.Requester);

        await transactions.RunAsync(async token =>
        {
            await messages.AppendAsync(message, token);
            await history.AppendAsync(AuditFactory.Create(ticket, TicketChangeType.MessageAdded, context.ActorId, now, context.CorrelationId), token);
            await outbox.AppendAsync(TicketOutbox.CommentCreated(ticket, message.Id, context, now), token);
        }, ct);

        return message;
    }
}
