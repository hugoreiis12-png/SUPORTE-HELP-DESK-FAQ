using Comprove.HelpDesk.Application.Abstractions;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Re-entrante: se já há transação corrente, reaproveita (consumidor com inbox + caso de uso).
public sealed class MongoTransactionRunner(IMongoClient client, MongoSessionContext sessionContext) : ITransactionRunner
{
    public async Task RunAsync(Func<CancellationToken, Task> work, CancellationToken ct)
    {
        if (sessionContext.Current is not null)
        {
            await work(ct);
            return;
        }

        using var session = await client.StartSessionAsync(cancellationToken: ct);
        sessionContext.Current = session;
        try
        {
            await session.WithTransactionAsync(
                async (_, token) =>
                {
                    await work(token);
                    return 0;
                },
                cancellationToken: ct);
        }
        finally
        {
            sessionContext.Current = null;
        }
    }
}
