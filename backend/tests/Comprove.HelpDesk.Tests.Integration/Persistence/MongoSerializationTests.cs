using Comprove.HelpDesk.Infrastructure.Persistence.Mongo;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Persistence;

// Não precisa de Mongo: verifica só o formato BSON gerado pelo driver.
public sealed class MongoSerializationTests
{
    private static readonly DateTimeOffset Instant = new(2026, 11, 25, 15, 5, 7, TimeSpan.Zero);

    public MongoSerializationTests() => MongoSerialization.EnsureConfigured();

    [Fact]
    public void DateTimeOffset_is_stored_as_bson_date()
    {
        var document = new TicketDocument { CreatedAt = Instant, SlaDeadline = Instant.AddHours(2) };

        var bson = document.ToBsonDocument();

        Assert.Equal(BsonType.DateTime, bson["created_at"].BsonType);
        Assert.Equal(BsonType.DateTime, bson["sla_deadline"].BsonType);
    }

    [Fact]
    public void Nullable_DateTimeOffset_stores_null_and_round_trips()
    {
        var bson = new TicketDocument { CreatedAt = Instant, SlaDeadline = null }.ToBsonDocument();
        Assert.True(bson["sla_deadline"].IsBsonNull);

        var restored = BsonSerializer.Deserialize<TicketDocument>(bson);
        Assert.Equal(Instant, restored.CreatedAt);
        Assert.Null(restored.SlaDeadline);
    }
}
