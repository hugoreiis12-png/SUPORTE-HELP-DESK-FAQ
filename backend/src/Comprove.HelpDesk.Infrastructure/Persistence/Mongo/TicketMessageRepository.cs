using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

public sealed class TicketMessageRepository(MongoContext mongo, MongoSessionContext sessionContext) : ITicketMessageRepository
{
    private IMongoCollection<CommentDocument> Comments =>
        mongo.Collection<CommentDocument>(MongoCollectionNames.TicketComments);

    public Task AppendAsync(TicketMessage message, CancellationToken ct) =>
        Comments.InsertInSessionAsync(sessionContext.Current, new CommentDocument
        {
            Id = message.Id,
            TicketId = message.TicketId,
            TenantId = message.TenantId.Value,
            AuthorExternalId = message.AuthorExternalId,
            AuthorName = message.AuthorName,
            AuthorRole = message.AuthorRole,
            Body = message.Body,
            Visibility = message.Visibility,
            CreatedAt = message.CreatedAt,
        }, ct);

    // Listagem para o solicitante: nunca inclui notas internas (B11).
    public async Task<IReadOnlyList<TicketMessage>> ListPublicAsync(TenantId tenant, string ticketId, CancellationToken ct)
    {
        var filter = Builders<CommentDocument>.Filter.Where(
            d => d.TenantId == tenant.Value && d.TicketId == ticketId && d.Visibility == MessageVisibility.Public);
        var documents = await Comments.Find(filter).SortBy(d => d.CreatedAt).ToListAsync(ct);
        return documents.Select(ToDomain).ToList();
    }

    // Visão do agente, em lote: uma consulta para todos os chamados da página.
    public async Task<IReadOnlyList<TicketMessage>> ListAllByTicketIdsAsync(
        TenantId tenant,
        IReadOnlyCollection<string> ticketIds,
        CancellationToken ct)
    {
        if (ticketIds.Count == 0)
        {
            return [];
        }

        var filter = Builders<CommentDocument>.Filter.And(
            Builders<CommentDocument>.Filter.Eq(d => d.TenantId, tenant.Value),
            Builders<CommentDocument>.Filter.In(d => d.TicketId, ticketIds));
        var documents = await Comments.Find(filter).SortBy(d => d.CreatedAt).ToListAsync(ct);
        return documents.Select(ToDomain).ToList();
    }

    private static TicketMessage ToDomain(CommentDocument d) => new(
        d.Id,
        d.TicketId,
        TenantId.From(d.TenantId),
        d.AuthorExternalId,
        d.Body,
        d.Visibility,
        d.CreatedAt,
        d.AuthorName,
        d.AuthorRole);
}
