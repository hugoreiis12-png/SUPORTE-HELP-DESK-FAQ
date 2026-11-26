using System.Text.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Contracts.Events;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Worker.Consumers;

// Lê o payload tipado e monta o contexto de sistema (ator "system", sem papéis) a partir do envelope.
public static class EnvelopeReader
{
    public static T ReadPayload<T>(EventEnvelope envelope) =>
        JsonSerializer.Deserialize<T>(envelope.PayloadJson)
        ?? throw new InvalidDataException($"Payload vazio em {envelope.EventType}.");

    public static RequestContext SystemContext(EventEnvelope envelope) =>
        new(
            TenantId.From(envelope.TenantId),
            RequestContext.SystemActor,
            Array.Empty<string>(),
            envelope.CorrelationId);
}
