using Comprove.HelpDesk.Infrastructure.Configuration;
using Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Índices de B07 e documento inicial de sla_policies. Idempotente. Chamado no start da API e do Worker.
public static class MongoBootstrap
{
    private const int DefaultCriticalHours = 2;
    private const int DefaultHighHours = 6;
    private const int DefaultMediumHours = 24;
    private const int DefaultLowHours = 48;

    public static async Task InitializeHelpDeskStorageAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var mongo = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var sla = scope.ServiceProvider.GetRequiredService<IOptions<SlaOptions>>().Value;

        await CreateTicketIndexesAsync(mongo, ct);
        await CreateSupportIndexesAsync(mongo, ct);
        await CreateFaqIndexesAsync(mongo, ct);
        await SeedSlaPolicyAsync(mongo, sla, ct);
    }

    private static async Task CreateTicketIndexesAsync(MongoContext mongo, CancellationToken ct)
    {
        var tickets = mongo.Collection<TicketDocument>(MongoCollectionNames.Tickets);
        var keys = Builders<TicketDocument>.IndexKeys;
        var models = new List<CreateIndexModel<TicketDocument>>
        {
            new(keys.Ascending(d => d.TenantId).Ascending(d => d.CreatedAt), new CreateIndexOptions { Name = "ix_tickets_tenant_created" }),
            new(keys.Ascending(d => d.TenantId).Ascending(d => d.Status).Ascending(d => d.UpdatedAt), new CreateIndexOptions { Name = "ix_tickets_tenant_status_updated" }),
            new(keys.Ascending(d => d.TenantId).Ascending("requester.external_user_id").Ascending(d => d.CreatedAt), new CreateIndexOptions { Name = "ix_tickets_tenant_requester_created" }),
            new(keys.Ascending(d => d.TenantId).Ascending(d => d.AssignedTo).Ascending(d => d.Status), new CreateIndexOptions { Name = "ix_tickets_tenant_assignee_status" }),
            new(keys.Ascending(d => d.TenantId).Ascending(d => d.QueueId).Ascending(d => d.Status), new CreateIndexOptions { Name = "ix_tickets_tenant_queue_status" }),
            new(keys.Ascending(d => d.Protocol), new CreateIndexOptions { Name = "ux_tickets_protocol", Unique = true }),
            new(keys.Ascending(d => d.IdempotencyKey), new CreateIndexOptions { Name = "ux_tickets_idempotency", Unique = true, Sparse = true }),
        };
        await tickets.Indexes.CreateManyAsync(models, ct);
    }

    private static async Task CreateSupportIndexesAsync(MongoContext mongo, CancellationToken ct)
    {
        var history = mongo.Collection<AuditDocument>(MongoCollectionNames.TicketHistory);
        await history.Indexes.CreateOneAsync(
            new CreateIndexModel<AuditDocument>(
                Builders<AuditDocument>.IndexKeys.Ascending(d => d.TenantId).Ascending(d => d.TicketId).Ascending(d => d.OccurredAt),
                new CreateIndexOptions { Name = "ix_history_tenant_ticket_time" }),
            cancellationToken: ct);

        var outbox = mongo.Collection<OutboxDocument>(MongoCollectionNames.OutboxEvents);
        await outbox.Indexes.CreateOneAsync(
            new CreateIndexModel<OutboxDocument>(
                Builders<OutboxDocument>.IndexKeys.Ascending(d => d.Status).Ascending(d => d.NextAttemptAt),
                new CreateIndexOptions { Name = "ix_outbox_status_next" }),
            cancellationToken: ct);

        var comments = mongo.Collection<CommentDocument>(MongoCollectionNames.TicketComments);
        await comments.Indexes.CreateOneAsync(
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys.Ascending(d => d.TenantId).Ascending(d => d.TicketId).Ascending(d => d.CreatedAt),
                new CreateIndexOptions { Name = "ix_comments_tenant_ticket_time" }),
            cancellationToken: ct);

        var attachments = mongo.Collection<AttachmentDocument>(MongoCollectionNames.TicketAttachments);
        await attachments.Indexes.CreateOneAsync(
            new CreateIndexModel<AttachmentDocument>(
                Builders<AttachmentDocument>.IndexKeys.Ascending(d => d.TenantId).Ascending(d => d.TicketId),
                new CreateIndexOptions { Name = "ix_attachments_tenant_ticket" }),
            cancellationToken: ct);
    }

    private static async Task CreateFaqIndexesAsync(MongoContext mongo, CancellationToken ct)
    {
        var faqs = mongo.Collection<FaqDocument>(MongoCollectionNames.Faqs);
        var keys = Builders<FaqDocument>.IndexKeys;
        await faqs.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<FaqDocument>(keys.Ascending(d => d.TenantId).Ascending(d => d.CreatedAt), new CreateIndexOptions { Name = "ix_faqs_tenant_created" }),
            new CreateIndexModel<FaqDocument>(keys.Ascending(d => d.TenantId).Ascending(d => d.ResolutionStatus).Ascending(d => d.CreatedAt), new CreateIndexOptions { Name = "ix_faqs_tenant_status_created" }),
            new CreateIndexModel<FaqDocument>(keys.Ascending(d => d.TenantId).Ascending(d => d.TicketId), new CreateIndexOptions { Name = "ux_faqs_tenant_ticket", Unique = true }),
        ], ct);

        var votes = mongo.Collection<FaqUpvoteDocument>(MongoCollectionNames.FaqUpvotes);
        await votes.Indexes.CreateOneAsync(
            new CreateIndexModel<FaqUpvoteDocument>(
                Builders<FaqUpvoteDocument>.IndexKeys.Ascending(d => d.TenantId).Ascending(d => d.UserId).Ascending(d => d.FaqId),
                new CreateIndexOptions { Name = "ix_faq_upvotes_tenant_user_faq" }),
            cancellationToken: ct);
    }

    // Inbox: _id = event_id já garante unicidade; nenhum índice extra necessário.
    private static async Task SeedSlaPolicyAsync(MongoContext mongo, SlaOptions sla, CancellationToken ct)
    {
        var policies = mongo.Collection<SlaPolicyDocument>(MongoCollectionNames.SlaPolicies);
        var filter = Builders<SlaPolicyDocument>.Filter.Eq(d => d.Id, "active");
        var update = Builders<SlaPolicyDocument>.Update
            .SetOnInsert(d => d.RulesVersion, sla.RulesVersion)
            .SetOnInsert(d => d.TargetsHours, new Dictionary<string, int>
            {
                ["Crítica"] = DefaultCriticalHours,
                ["Alta"] = DefaultHighHours,
                ["Média"] = DefaultMediumHours,
                ["Baixa"] = DefaultLowHours,
            });
        await policies.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }
}
