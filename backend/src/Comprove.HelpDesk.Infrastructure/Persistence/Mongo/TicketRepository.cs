using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.ValueObjects;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

public sealed class TicketRepository(MongoContext mongo, MongoSessionContext sessionContext) : ITicketRepository
{
    private const string ProtocolIndexName = "ux_tickets_protocol";
    private const string IdempotencyIndexName = "ux_tickets_idempotency";
    private const int DuplicateKeyErrorCode = 11000;

    private IMongoCollection<TicketDocument> Tickets =>
        mongo.Collection<TicketDocument>(MongoCollectionNames.Tickets);

    public async Task<Ticket?> FindAsync(TenantId tenant, string ticketId, CancellationToken ct)
    {
        var filter = Builders<TicketDocument>.Filter.Where(d => d.Id == ticketId && d.TenantId == tenant.Value);
        var document = await Tickets.Find(filter).FirstOrDefaultAsync(ct);
        return document is null ? null : new Ticket(document.ToDomain());
    }

    public async Task<Ticket?> FindByIdempotencyKeyAsync(TenantId tenant, string idempotencyKey, CancellationToken ct)
    {
        var filter = Builders<TicketDocument>.Filter.Where(d => d.TenantId == tenant.Value && d.IdempotencyKey == idempotencyKey);
        var document = await Tickets.Find(filter).FirstOrDefaultAsync(ct);
        return document is null ? null : new Ticket(document.ToDomain());
    }

    public async Task InsertAsync(Ticket ticket, CancellationToken ct)
    {
        try
        {
            await Tickets.InsertInSessionAsync(sessionContext.Current, TicketDocument.FromDomain(ticket.Snapshot()), ct);
        }
        catch (MongoWriteException ex) when (IsDuplicate(ex, ProtocolIndexName))
        {
            throw new ProtocolCollisionException();
        }
        catch (MongoWriteException ex) when (IsDuplicate(ex, IdempotencyIndexName))
        {
            throw new ConcurrencyConflictException();
        }
    }

    public async Task UpdateAsync(Ticket ticket, long expectedVersion, CancellationToken ct)
    {
        var state = ticket.Snapshot();
        var filter = Builders<TicketDocument>.Filter.Where(
            d => d.Id == state.Id && d.TenantId == state.TenantId.Value && d.Version == expectedVersion);
        var result = await Tickets.ReplaceInSessionAsync(sessionContext.Current, filter, TicketDocument.FromDomain(state), ct);
        if (result.MatchedCount == 0)
        {
            throw new ConcurrencyConflictException();
        }
    }

    public async Task<TicketPage> ListAsync(TicketQuery query, CancellationToken ct)
    {
        var filter = BuildFilter(query);
        var sort = Builders<TicketDocument>.Sort.Descending(d => d.CreatedAt);
        var documents = await Tickets
            .Find(filter)
            .Sort(sort)
            .Limit(query.PageSize + 1)
            .ToListAsync(ct);

        var hasMore = documents.Count > query.PageSize;
        var page = documents.Take(query.PageSize).Select(d => new Ticket(d.ToDomain())).ToList();
        var nextCursor = hasMore ? documents[query.PageSize - 1].CreatedAt : (DateTimeOffset?)null;
        return new TicketPage(page, nextCursor);
    }

    private static FilterDefinition<TicketDocument> BuildFilter(TicketQuery query)
    {
        var builder = Builders<TicketDocument>.Filter;
        var filters = new List<FilterDefinition<TicketDocument>> { builder.Eq(d => d.TenantId, query.TenantId.Value) };

        if (query.Status is { } status)
        {
            filters.Add(builder.Eq(d => d.Status, status));
        }

        if (query.QueueId is { } queueId)
        {
            filters.Add(builder.Eq(d => d.QueueId, queueId));
        }

        if (query.AssignedTo is { } assignedTo)
        {
            filters.Add(builder.Eq(d => d.AssignedTo, assignedTo));
        }

        if (query.RequesterExternalUserId is { } requester)
        {
            filters.Add(builder.Eq(d => d.Requester.ExternalUserId, requester));
        }

        if (query.CreatedBefore is { } before)
        {
            filters.Add(builder.Lt(d => d.CreatedAt, before));
        }

        return builder.And(filters);
    }

    private static bool IsDuplicate(MongoWriteException ex, string indexName) =>
        ex.WriteError.Code == DuplicateKeyErrorCode && ex.Message.Contains(indexName, StringComparison.Ordinal);
}
