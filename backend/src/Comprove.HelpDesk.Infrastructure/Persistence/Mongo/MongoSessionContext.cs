using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Sessão da transação corrente. Repositórios gravam nela quando existe (Outbox na mesma transação, ADR-004).
public sealed class MongoSessionContext
{
    private readonly AsyncLocal<IClientSessionHandle?> _current = new();

    public IClientSessionHandle? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
