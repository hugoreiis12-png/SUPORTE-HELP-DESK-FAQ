using Comprove.HelpDesk.Domain.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;

// ticket_history (B06).
[BsonIgnoreExtraElements]
public sealed class AuditDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("ticket_id")]
    public string TicketId { get; set; } = string.Empty;

    [BsonElement("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("change_type")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public TicketChangeType ChangeType { get; set; }

    [BsonElement("from_status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public TicketStatus? FromStatus { get; set; }

    [BsonElement("to_status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public TicketStatus? ToStatus { get; set; }

    [BsonElement("actor")]
    public string Actor { get; set; } = string.Empty;

    [BsonElement("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; }

    [BsonElement("reason")]
    public string? Reason { get; set; }

    [BsonElement("correlation_id")]
    public string CorrelationId { get; set; } = string.Empty;

    [BsonElement("idempotency_key")]
    public string? IdempotencyKey { get; set; }
}

// ticket_comments (B06).
[BsonIgnoreExtraElements]
public sealed class CommentDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("ticket_id")]
    public string TicketId { get; set; } = string.Empty;

    [BsonElement("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("author_external_id")]
    public string AuthorExternalId { get; set; } = string.Empty;

    [BsonElement("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [BsonElement("author_role")]
    public string AuthorRole { get; set; } = string.Empty;

    [BsonElement("body")]
    public string Body { get; set; } = string.Empty;

    [BsonElement("visibility")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public MessageVisibility Visibility { get; set; }

    [BsonElement("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

// ticket_attachments (B06). Só metadados.
[BsonIgnoreExtraElements]
public sealed class AttachmentDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("ticket_id")]
    public string TicketId { get; set; } = string.Empty;

    [BsonElement("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("object_key")]
    public string ObjectKey { get; set; } = string.Empty;

    [BsonElement("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [BsonElement("size")]
    public long Size { get; set; }

    [BsonElement("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

// outbox_events (ADR-004).
[BsonIgnoreExtraElements]
public sealed class OutboxDocument
{
    [BsonId]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("event_type")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("routing_key")]
    public string RoutingKey { get; set; } = string.Empty;

    [BsonElement("schema_version")]
    public int SchemaVersion { get; set; }

    [BsonElement("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("correlation_id")]
    public string CorrelationId { get; set; } = string.Empty;

    [BsonElement("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; }

    [BsonElement("payload")]
    public string Payload { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = OutboxStatus.Pending;

    [BsonElement("attempts")]
    public int Attempts { get; set; }

    [BsonElement("next_attempt_at")]
    public DateTimeOffset NextAttemptAt { get; set; }

    [BsonElement("last_error")]
    public string? LastError { get; set; }
}

public static class OutboxStatus
{
    public const string Pending = "pending";
    public const string Published = "published";
    public const string Failed = "failed";
}

// processed_messages (inbox, B15). _id = event_id: unicidade garantida pelo próprio índice do _id.
[BsonIgnoreExtraElements]
public sealed class ProcessedMessageDocument
{
    [BsonId]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("consumer")]
    public string Consumer { get; set; } = string.Empty;

    [BsonElement("processed_at")]
    public DateTimeOffset ProcessedAt { get; set; }
}

// protocol_sequences (B08): uma linha por ano.
[BsonIgnoreExtraElements]
public sealed class ProtocolSequenceDocument
{
    [BsonId]
    public string Year { get; set; } = string.Empty;

    [BsonElement("seq")]
    public long Seq { get; set; }
}

// support_queues (B17): categorias atendidas por fila.
[BsonIgnoreExtraElements]
public sealed class SupportQueueDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("category_ids")]
    public List<string> CategoryIds { get; set; } = [];
}

// support_teams (B17): rodízio por fila (next_index incrementado atomicamente).
[BsonIgnoreExtraElements]
public sealed class SupportTeamDocument
{
    [BsonId]
    public string QueueId { get; set; } = string.Empty;

    [BsonElement("agents")]
    public List<string> Agents { get; set; } = [];

    [BsonElement("next_index")]
    public long NextIndex { get; set; }
}

// sla_policies (B06): documento _id = "active".
[BsonIgnoreExtraElements]
public sealed class SlaPolicyDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("rules_version")]
    public string RulesVersion { get; set; } = string.Empty;

    [BsonElement("targets_hours")]
    public Dictionary<string, int> TargetsHours { get; set; } = new();
}
