using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.ValueObjects;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

public sealed class TicketHistoryRepository(MongoContext mongo, MongoSessionContext sessionContext) : ITicketHistoryRepository
{
    private IMongoCollection<AuditDocument> History =>
        mongo.Collection<AuditDocument>(MongoCollectionNames.TicketHistory);

    public Task AppendAsync(AuditEntry entry, CancellationToken ct) =>
        History.InsertInSessionAsync(sessionContext.Current, new AuditDocument
        {
            Id = entry.Id,
            TicketId = entry.TicketId,
            TenantId = entry.TenantId.Value,
            ChangeType = entry.ChangeType,
            FromStatus = entry.FromStatus,
            ToStatus = entry.ToStatus,
            Actor = entry.Actor,
            OccurredAt = entry.OccurredAt,
            Reason = entry.Reason,
            CorrelationId = entry.CorrelationId,
            IdempotencyKey = entry.IdempotencyKey,
        }, ct);

    public async Task<bool> ExistsWithIdempotencyKeyAsync(TenantId tenant, string ticketId, string idempotencyKey, CancellationToken ct)
    {
        var filter = Builders<AuditDocument>.Filter.Where(
            d => d.TenantId == tenant.Value && d.TicketId == ticketId && d.IdempotencyKey == idempotencyKey);
        return await History.Find(filter).AnyAsync(ct);
    }
}
