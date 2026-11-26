using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Incremento atômico por ano. Fora de transação de propósito: número reservado não volta atrás.
public sealed class ProtocolSequenceRepository(MongoContext mongo) : IProtocolSequence
{
    public async Task<long> NextAsync(int year, CancellationToken ct)
    {
        var collection = mongo.Collection<ProtocolSequenceDocument>(MongoCollectionNames.ProtocolSequences);
        var filter = Builders<ProtocolSequenceDocument>.Filter.Eq(d => d.Year, year.ToString());
        var update = Builders<ProtocolSequenceDocument>.Update.Inc(d => d.Seq, 1L);
        var options = new FindOneAndUpdateOptions<ProtocolSequenceDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };
        var document = await collection.FindOneAndUpdateAsync(filter, update, options, ct);
        return document.Seq;
    }
}
