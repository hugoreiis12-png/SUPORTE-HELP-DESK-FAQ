using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Inbox (B15). Deve ser chamado dentro da transação do efeito: claim e efeito commitam juntos.
public sealed class ProcessedMessageRepository(MongoContext mongo, MongoSessionContext sessionContext) : IProcessedMessageRepository
{
    private IMongoCollection<ProcessedMessageDocument> Processed =>
        mongo.Collection<ProcessedMessageDocument>(MongoCollectionNames.ProcessedMessages);

    public async Task<bool> TryClaimAsync(string eventId, string consumer, DateTimeOffset processedAt, CancellationToken ct)
    {
        var session = sessionContext.Current;
        var filter = Builders<ProcessedMessageDocument>.Filter.Eq(d => d.EventId, eventId);
        var exists = session is null
            ? await Processed.Find(filter).AnyAsync(ct)
            : await Processed.Find(session, filter).AnyAsync(ct);
        if (exists)
        {
            return false;
        }

        await Processed.InsertInSessionAsync(session, new ProcessedMessageDocument
        {
            EventId = eventId,
            Consumer = consumer,
            ProcessedAt = processedAt,
        }, ct);
        return true;
    }
}
