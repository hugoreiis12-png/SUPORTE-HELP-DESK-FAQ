using System.Text.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Services;

public static class OutboxFactory
{
    private const int SchemaVersion = 1;

    public static OutboxMessage Create<TPayload>(
        string eventType,
        string routingKey,
        TenantId tenant,
        string correlationId,
        DateTimeOffset now,
        TPayload payload)
    {
        var envelope = new EventEnvelope(
            EventId: Guid.NewGuid().ToString("N"),
            EventType: eventType,
            SchemaVersion: SchemaVersion,
            TenantId: tenant.Value,
            CorrelationId: correlationId,
            OccurredAt: now,
            PayloadJson: JsonSerializer.Serialize(payload));
        return new OutboxMessage(envelope, routingKey);
    }
}
