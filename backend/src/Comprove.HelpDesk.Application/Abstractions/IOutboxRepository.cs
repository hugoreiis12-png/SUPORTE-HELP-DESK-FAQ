using Comprove.HelpDesk.Contracts.Events;

namespace Comprove.HelpDesk.Application.Abstractions;

// Mensagem pronta para publicação: envelope + routing key do RabbitMQ.
public sealed record OutboxMessage(EventEnvelope Envelope, string RoutingKey);

public sealed record OutboxRecord(string EventId, OutboxMessage Message, int Attempts);

public interface IOutboxRepository
{
    // Gravado na mesma transação da mudança de negócio (ADR-004).
    Task AppendAsync(OutboxMessage message, CancellationToken ct);

    Task<IReadOnlyList<OutboxRecord>> LoadPendingAsync(int batchSize, DateTimeOffset now, CancellationToken ct);

    Task MarkPublishedAsync(string eventId, CancellationToken ct);

    Task MarkFailedAsync(string eventId, int attempts, DateTimeOffset nextAttemptAt, string error, CancellationToken ct);
}
