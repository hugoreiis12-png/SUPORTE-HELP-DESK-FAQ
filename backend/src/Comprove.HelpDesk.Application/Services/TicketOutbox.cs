using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Contracts.Integration;
using Comprove.HelpDesk.Domain.Entities;

namespace Comprove.HelpDesk.Application.Services;

// Eventos de outbox dos casos de uso de ticket. Mesma transação do efeito (ADR-004).
public static class TicketOutbox
{
    public static OutboxMessage Created(Ticket ticket, RequestContext context, DateTimeOffset now)
    {
        var state = ticket.Snapshot();
        return OutboxFactory.Create(
            TicketEventTypes.Created,
            RabbitTopology.TicketCreatedRoutingKey,
            state.TenantId,
            context.CorrelationId,
            now,
            new TicketCreatedPayload(state.Id, state.Protocol.Value, state.CategoryId));
    }

    public static OutboxMessage StatusChanged(Ticket ticket, StatusChange change, RequestContext context, DateTimeOffset now)
    {
        var state = ticket.Snapshot();
        return OutboxFactory.Create(
            TicketEventTypes.StatusChanged,
            RabbitTopology.TicketStatusChangedRoutingKey,
            state.TenantId,
            context.CorrelationId,
            now,
            new TicketStatusChangedPayload(state.Id, state.Protocol.Value, change.From.ToString(), change.To.ToString()));
    }

    public static OutboxMessage Updated(Ticket ticket, RequestContext context, DateTimeOffset now)
    {
        var state = ticket.Snapshot();
        return OutboxFactory.Create(
            TicketEventTypes.Updated,
            RabbitTopology.TicketUpdatedRoutingKey,
            state.TenantId,
            context.CorrelationId,
            now,
            new TicketUpdatedPayload(state.Id, state.Protocol.Value, state.CategoryId));
    }

    // Um único evento de resolução; a sincronização FAQ reage a ele, não a um segundo disparo (B18).
    public static OutboxMessage Resolved(Ticket ticket, RequestContext context, DateTimeOffset now)
    {
        var state = ticket.Snapshot();
        return OutboxFactory.Create(
            TicketEventTypes.Resolved,
            RabbitTopology.TicketResolvedRoutingKey,
            state.TenantId,
            context.CorrelationId,
            now,
            new TicketResolvedPayload(state.Id, state.Protocol.Value, now));
    }

    public static OutboxMessage CommentCreated(Ticket ticket, string messageId, RequestContext context, DateTimeOffset now)
    {
        var state = ticket.Snapshot();
        return OutboxFactory.Create(
            TicketEventTypes.CommentCreated,
            RabbitTopology.CommentCreatedRoutingKey,
            state.TenantId,
            context.CorrelationId,
            now,
            new CommentCreatedPayload(state.Id, messageId));
    }
}
