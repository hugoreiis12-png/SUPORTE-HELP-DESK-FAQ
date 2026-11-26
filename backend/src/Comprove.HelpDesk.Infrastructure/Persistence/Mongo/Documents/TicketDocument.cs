using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using DomainTenantId = Comprove.HelpDesk.Domain.ValueObjects.TenantId;
using Comprove.HelpDesk.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class TicketDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [BsonElement("requester")]
    public RequesterDocument Requester { get; set; } = new();

    [BsonElement("category_id")]
    public string CategoryId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("impact")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public ImpactLevel Impact { get; set; }

    [BsonElement("scope")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public ScopeLevel Scope { get; set; }

    [BsonElement("frequency")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public FrequencyLevel Frequency { get; set; }

    [BsonElement("urgency")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    [BsonIgnoreIfNull]
    public UrgencyLevel? Urgency { get; set; }

    [BsonElement("severity")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public Severity? Severity { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public TicketStatus Status { get; set; }

    [BsonElement("queue_id")]
    public string? QueueId { get; set; }

    [BsonElement("assigned_to")]
    public string? AssignedTo { get; set; }

    [BsonElement("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [BsonElement("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [BsonElement("sla_deadline")]
    public DateTimeOffset? SlaDeadline { get; set; }

    [BsonElement("paused_at")]
    public DateTimeOffset? PausedAt { get; set; }

    [BsonElement("resolved_at")]
    public DateTimeOffset? ResolvedAt { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    // Null não é gravado: o índice único esparso só vale para chamados com chave.
    [BsonElement("idempotency_key")]
    [BsonIgnoreIfNull]
    public string? IdempotencyKey { get; set; }

    public static TicketDocument FromDomain(TicketSnapshot state) => new()
    {
        Id = state.Id,
        TenantId = state.TenantId.Value,
        Protocol = state.Protocol.Value,
        Requester = RequesterDocument.FromDomain(state.RequesterExternalUserId, state.Requester),
        CategoryId = state.CategoryId,
        Title = state.Title,
        Description = state.Description,
        Impact = state.Impact,
        Scope = state.Scope,
        Frequency = state.Frequency,
        Urgency = state.Urgency,
        Severity = state.Severity,
        Status = state.Status,
        QueueId = state.QueueId,
        AssignedTo = state.AssignedTo,
        CreatedAt = state.CreatedAt,
        UpdatedAt = state.UpdatedAt,
        SlaDeadline = state.SlaDeadline,
        PausedAt = state.PausedAt,
        ResolvedAt = state.ResolvedAt,
        Version = state.Version,
        IdempotencyKey = state.IdempotencyKey,
    };

    public TicketSnapshot ToDomain() => new(
        Id: Id,
        TenantId: DomainTenantId.From(TenantId),
        Protocol: ProtocolNumber.Parse(Protocol),
        RequesterExternalUserId: Requester.ExternalUserId,
        CategoryId: CategoryId,
        Title: Title,
        Description: Description,
        Impact: Impact,
        Scope: Scope,
        Frequency: Frequency,
        Urgency: Urgency,
        Severity: Severity,
        Status: Status,
        QueueId: QueueId,
        AssignedTo: AssignedTo,
        CreatedAt: CreatedAt,
        UpdatedAt: UpdatedAt,
        SlaDeadline: SlaDeadline,
        PausedAt: PausedAt,
        ResolvedAt: ResolvedAt,
        Version: Version,
        IdempotencyKey: IdempotencyKey,
        Requester: Requester.ToProfile());
}

public sealed class RequesterDocument
{
    [BsonElement("external_user_id")]
    public string ExternalUserId { get; set; } = string.Empty;

    [BsonElement("name")]
    [BsonIgnoreIfNull]
    public string? Name { get; set; }

    [BsonElement("phone")]
    [BsonIgnoreIfNull]
    public string? Phone { get; set; }

    [BsonElement("roles")]
    [BsonIgnoreIfNull]
    public List<string>? Roles { get; set; }

    public static RequesterDocument FromDomain(string externalUserId, RequesterProfile? profile) => new()
    {
        ExternalUserId = externalUserId,
        Name = profile?.Name,
        Phone = profile?.Phone,
        Roles = profile?.Roles.ToList(),
    };

    // Chamados antigos não têm perfil gravado.
    public RequesterProfile? ToProfile() =>
        Name is null && Phone is null && Roles is null ? null : new RequesterProfile(Name, Phone, Roles ?? []);
}
