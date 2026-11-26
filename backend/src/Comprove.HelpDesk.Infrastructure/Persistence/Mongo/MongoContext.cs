using Comprove.HelpDesk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Única porta para a database do Help Desk (ADR-003). Nunca aponta para a database do ERP.
public sealed class MongoContext(IMongoClient client, IOptions<MongoOptions> options)
{
    public IMongoDatabase Database { get; } = client.GetDatabase(options.Value.DatabaseName);

    public IMongoCollection<T> Collection<T>(string name) => Database.GetCollection<T>(name);
}
