using System.Text.RegularExpressions;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

public sealed class FaqRepository(MongoContext mongo, MongoSessionContext sessionContext) : IFaqRepository
{
    private IMongoCollection<FaqDocument> Faqs => mongo.Collection<FaqDocument>(MongoCollectionNames.Faqs);

    private IMongoCollection<FaqUpvoteDocument> Votes => mongo.Collection<FaqUpvoteDocument>(MongoCollectionNames.FaqUpvotes);

    public async Task<Faq?> FindAsync(TenantId tenant, string faqId, CancellationToken ct)
    {
        var filter = Builders<FaqDocument>.Filter.Where(d => d.Id == faqId && d.TenantId == tenant.Value);
        return (await Faqs.Find(filter).FirstOrDefaultAsync(ct))?.ToDomain();
    }

    public async Task<Faq?> FindByTicketIdAsync(TenantId tenant, string ticketId, CancellationToken ct)
    {
        var filter = Builders<FaqDocument>.Filter.Where(d => d.TenantId == tenant.Value && d.TicketId == ticketId);
        return (await Faqs.Find(filter).FirstOrDefaultAsync(ct))?.ToDomain();
    }

    public async Task<IReadOnlyDictionary<string, Faq>> FindByTicketIdsAsync(
        TenantId tenant,
        IReadOnlyCollection<string> ticketIds,
        CancellationToken ct)
    {
        var filter = Builders<FaqDocument>.Filter.And(
            Builders<FaqDocument>.Filter.Eq(d => d.TenantId, tenant.Value),
            Builders<FaqDocument>.Filter.In(d => d.TicketId, ticketIds));
        var documents = await Faqs.Find(filter).ToListAsync(ct);
        return documents.ToDictionary(d => d.TicketId, d => d.ToDomain());
    }

    public async Task<long> NextNumberAsync(TenantId tenant, CancellationToken ct)
    {
        var counters = mongo.Collection<FaqCounterDocument>(MongoCollectionNames.FaqCounters);
        var options = new FindOneAndUpdateOptions<FaqCounterDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
        var counter = await counters.FindOneAndUpdateAsync(
            Builders<FaqCounterDocument>.Filter.Eq(d => d.TenantId, tenant.Value),
            Builders<FaqCounterDocument>.Update.Inc(d => d.Seq, 1L),
            options,
            ct);
        return counter.Seq;
    }

    public Task InsertAsync(Faq faq, CancellationToken ct) =>
        Faqs.InsertInSessionAsync(sessionContext.Current, FaqDocument.FromDomain(faq), ct);

    public Task UpdateResolutionAsync(Faq faq, CancellationToken ct) =>
        Faqs.UpdateInSessionAsync(
            sessionContext.Current,
            Builders<FaqDocument>.Filter.Where(d => d.Id == faq.Id && d.TenantId == faq.TenantId.Value),
            Builders<FaqDocument>.Update
                .Set(d => d.ResolutionStatus, faq.ResolutionStatus)
                .Set(d => d.ResolutionText, faq.ResolutionText)
                .Set(d => d.UpdatedAt, faq.UpdatedAt),
            ct);

    public Task UpdateStatusByTicketAsync(TenantId tenant, string ticketId, FaqResolutionStatus status, DateTimeOffset now, CancellationToken ct) =>
        Faqs.UpdateInSessionAsync(
            sessionContext.Current,
            Builders<FaqDocument>.Filter.Where(d => d.TenantId == tenant.Value && d.TicketId == ticketId),
            Builders<FaqDocument>.Update.Set(d => d.ResolutionStatus, status).Set(d => d.UpdatedAt, now),
            ct);

    public async Task<Page<Faq>> ListAsync(FaqQuery query, CancellationToken ct)
    {
        var documents = await Faqs
            .Find(BuildFilter(query))
            .SortByDescending(d => d.CreatedAt)
            .Limit(query.PageSize + 1)
            .ToListAsync(ct);

        var hasMore = documents.Count > query.PageSize;
        var items = documents.Take(query.PageSize).Select(d => d.ToDomain()).ToList();
        return new Page<Faq>(items, hasMore ? documents[query.PageSize - 1].CreatedAt : null);
    }

    public async Task<FaqUpvote> ToggleUpvoteAsync(TenantId tenant, string faqId, string userId, DateTimeOffset now, CancellationToken ct)
    {
        var voteId = $"{tenant.Value}:{faqId}:{userId}";
        var removed = await Votes.DeleteOneAsync(Builders<FaqUpvoteDocument>.Filter.Eq(d => d.Id, voteId), ct);
        if (removed.DeletedCount == 1)
        {
            return new FaqUpvote(await AdjustAskCountAsync(tenant, faqId, -1, ct), false);
        }

        var vote = new FaqUpvoteDocument { Id = voteId, TenantId = tenant.Value, FaqId = faqId, UserId = userId, CreatedAt = now };
        await Votes.InsertOneAsync(vote, cancellationToken: ct);
        return new FaqUpvote(await AdjustAskCountAsync(tenant, faqId, 1, ct), true);
    }

    public async Task<IReadOnlySet<string>> UpvotedIdsAsync(
        TenantId tenant,
        string userId,
        IReadOnlyCollection<string> faqIds,
        CancellationToken ct)
    {
        if (faqIds.Count == 0)
        {
            return new HashSet<string>();
        }

        var filter = Builders<FaqUpvoteDocument>.Filter.And(
            Builders<FaqUpvoteDocument>.Filter.Eq(d => d.TenantId, tenant.Value),
            Builders<FaqUpvoteDocument>.Filter.Eq(d => d.UserId, userId),
            Builders<FaqUpvoteDocument>.Filter.In(d => d.FaqId, faqIds));
        var votes = await Votes.Find(filter).Project(d => d.FaqId).ToListAsync(ct);
        return votes.ToHashSet();
    }

    private async Task<int> AdjustAskCountAsync(TenantId tenant, string faqId, int delta, CancellationToken ct)
    {
        var options = new FindOneAndUpdateOptions<FaqDocument> { ReturnDocument = ReturnDocument.After };
        var updated = await Faqs.FindOneAndUpdateAsync(
            Builders<FaqDocument>.Filter.Where(d => d.Id == faqId && d.TenantId == tenant.Value),
            Builders<FaqDocument>.Update.Inc(d => d.AskCount, delta),
            options,
            ct);
        return Math.Max(0, updated.AskCount);
    }

    private static FilterDefinition<FaqDocument> BuildFilter(FaqQuery query)
    {
        var builder = Builders<FaqDocument>.Filter;
        var filters = new List<FilterDefinition<FaqDocument>> { builder.Eq(d => d.TenantId, query.TenantId.Value) };

        if (query.Status is { } status)
        {
            filters.Add(builder.Eq(d => d.ResolutionStatus, status));
        }

        if (query.CreatedBefore is { } before)
        {
            filters.Add(builder.Lt(d => d.CreatedAt, before));
        }

        if (query.Search is { } search)
        {
            filters.Add(SearchFilter(search));
        }

        return builder.And(filters);
    }

    // Busca textual simples e segura: o termo é escapado, nunca interpretado como regex.
    private static FilterDefinition<FaqDocument> SearchFilter(string search)
    {
        var builder = Builders<FaqDocument>.Filter;
        var pattern = new BsonRegularExpression(Regex.Escape(search), "i");
        return builder.Or(
            builder.Regex(d => d.Title, pattern),
            builder.Regex(d => d.ProblemDescription, pattern),
            builder.Regex(d => d.Code, pattern));
    }
}
