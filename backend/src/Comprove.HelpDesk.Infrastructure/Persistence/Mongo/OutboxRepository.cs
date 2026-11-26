using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

public sealed class OutboxRepository(MongoContext mongo, MongoSessionContext sessionContext) : IOutboxRepository
{
    private const int MaxAttempts = 10;

    private IMongoCollection<OutboxDocument> Outbox =>
        mongo.Collection<OutboxDocument>(MongoCollectionNames.OutboxEvents);

    public Task AppendAsync(OutboxMessage message, CancellationToken ct)
    {
        var envelope = message.Envelope;
        var document = new OutboxDocument
        {
            EventId = envelope.EventId,
            EventType = envelope.EventType,
            RoutingKey = message.RoutingKey,
            SchemaVersion = envelope.SchemaVersion,
            TenantId = envelope.TenantId,
            CorrelationId = envelope.CorrelationId,
            OccurredAt = envelope.OccurredAt,
            Payload = envelope.PayloadJson,
            NextAttemptAt = envelope.OccurredAt,
        };
        return Outbox.InsertInSessionAsync(sessionContext.Current, document, ct);
    }

    public async Task<IReadOnlyList<OutboxRecord>> LoadPendingAsync(int batchSize, DateTimeOffset now, CancellationToken ct)
    {
        var filter = Builders<OutboxDocument>.Filter.Where(
            d => d.Status == OutboxStatus.Pending && d.NextAttemptAt <= now);
        var sort = Builders<OutboxDocument>.Sort.Ascending(d => d.OccurredAt);
        var documents = await Outbox.Find(filter).Sort(sort).Limit(batchSize).ToListAsync(ct);
        return documents.Select(ToRecord).ToList();
    }

    public Task MarkPublishedAsync(string eventId, CancellationToken ct)
    {
        var filter = Builders<OutboxDocument>.Filter.Eq(d => d.EventId, eventId);
        var update = Builders<OutboxDocument>.Update.Set(d => d.Status, OutboxStatus.Published);
        return Outbox.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public Task MarkFailedAsync(string eventId, int attempts, DateTimeOffset nextAttemptAt, string error, CancellationToken ct)
    {
        var status = attempts >= MaxAttempts ? OutboxStatus.Failed : OutboxStatus.Pending;
        var filter = Builders<OutboxDocument>.Filter.Eq(d => d.EventId, eventId);
        var update = Builders<OutboxDocument>.Update
            .Set(d => d.Status, status)
            .Set(d => d.Attempts, attempts)
            .Set(d => d.NextAttemptAt, nextAttemptAt)
            .Set(d => d.LastError, error);
        return Outbox.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    private static OutboxRecord ToRecord(OutboxDocument document)
    {
        var envelope = new Contracts.Events.EventEnvelope(
            document.EventId,
            document.EventType,
            document.SchemaVersion,
            document.TenantId,
            document.CorrelationId,
            document.OccurredAt,
            document.Payload);
        return new OutboxRecord(document.EventId, new OutboxMessage(envelope, document.RoutingKey), document.Attempts);
    }
}
