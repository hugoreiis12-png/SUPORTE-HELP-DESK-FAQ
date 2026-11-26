using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.ValueObjects;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

public sealed class AttachmentRepository(MongoContext mongo, MongoSessionContext sessionContext) : IAttachmentRepository
{
    private IMongoCollection<AttachmentDocument> Attachments =>
        mongo.Collection<AttachmentDocument>(MongoCollectionNames.TicketAttachments);

    public Task InsertAsync(TicketAttachment attachment, CancellationToken ct) =>
        Attachments.InsertInSessionAsync(
            sessionContext.Current,
            new AttachmentDocument
            {
                Id = attachment.Id,
                TicketId = attachment.TicketId,
                TenantId = attachment.TenantId.Value,
                ObjectKey = attachment.ObjectKey,
                ContentType = attachment.ContentType,
                Size = attachment.SizeBytes,
                CreatedBy = attachment.CreatedBy,
                CreatedAt = attachment.CreatedAt,
            },
            ct);

    public async Task<IReadOnlyList<TicketAttachment>> ListByTicketIdsAsync(
        TenantId tenant,
        IReadOnlyCollection<string> ticketIds,
        CancellationToken ct)
    {
        var filter = Builders<AttachmentDocument>.Filter.And(
            Builders<AttachmentDocument>.Filter.Eq(d => d.TenantId, tenant.Value),
            Builders<AttachmentDocument>.Filter.In(d => d.TicketId, ticketIds));
        var documents = await Attachments.Find(filter).ToListAsync(ct);
        return documents.Select(ToDomain).ToList();
    }

    // O último segmento da chave é o _id do anexo: a busca usa o índice do _id.
    public async Task<TicketAttachment?> FindByObjectKeyAsync(string objectKey, CancellationToken ct)
    {
        var id = objectKey[(objectKey.LastIndexOf('/') + 1)..];
        var filter = Builders<AttachmentDocument>.Filter.Where(d => d.Id == id && d.ObjectKey == objectKey);
        var document = await Attachments.Find(filter).FirstOrDefaultAsync(ct);
        return document is null ? null : ToDomain(document);
    }

    private static TicketAttachment ToDomain(AttachmentDocument d) =>
        new(d.Id, d.TicketId, TenantId.From(d.TenantId), d.ObjectKey, d.ContentType, d.Size, d.CreatedBy, d.CreatedAt);
}
