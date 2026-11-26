using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Sem isto o driver grava DateTimeOffset como subdocumento { DateTime, Ticks, Offset }.
// Como data BSON, índices TTL, ordenação e relatórios funcionam. O offset não é guardado: o sistema trabalha em UTC.
public static class MongoSerialization
{
    private static readonly object Gate = new();
    private static bool _configured;

    // Idempotente. Deve rodar antes de qualquer uso do driver (chamado no registro da infraestrutura).
    public static void EnsureConfigured()
    {
        lock (Gate)
        {
            if (_configured)
            {
                return;
            }

            BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.DateTime));
            _configured = true;
        }
    }
}
