using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Escrita dentro da sessão corrente quando existir, sem sessão caso contrário.
public static class MongoSessionExtensions
{
    public static Task InsertInSessionAsync<T>(this IMongoCollection<T> collection, IClientSessionHandle? session, T document, CancellationToken ct) =>
        session is null
            ? collection.InsertOneAsync(document, null, ct)
            : collection.InsertOneAsync(session, document, null, ct);

    public static Task<ReplaceOneResult> ReplaceInSessionAsync<T>(
        this IMongoCollection<T> collection,
        IClientSessionHandle? session,
        FilterDefinition<T> filter,
        T replacement,
        CancellationToken ct) =>
        session is null
            ? collection.ReplaceOneAsync(filter, replacement, new ReplaceOptions(), ct)
            : collection.ReplaceOneAsync(session, filter, replacement, new ReplaceOptions(), ct);

    public static Task<UpdateResult> UpdateInSessionAsync<T>(
        this IMongoCollection<T> collection,
        IClientSessionHandle? session,
        FilterDefinition<T> filter,
        UpdateDefinition<T> update,
        CancellationToken ct) =>
        session is null
            ? collection.UpdateOneAsync(filter, update, cancellationToken: ct)
            : collection.UpdateOneAsync(session, filter, update, cancellationToken: ct);
}
