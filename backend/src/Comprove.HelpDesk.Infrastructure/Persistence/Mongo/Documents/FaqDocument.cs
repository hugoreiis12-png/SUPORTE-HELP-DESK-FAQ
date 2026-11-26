using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using DomainTenantId = Comprove.HelpDesk.Domain.ValueObjects.TenantId;

namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo.Documents;

// faqs: um FAQ por chamado (índice único tenant + ticket_id).
[BsonIgnoreExtraElements]
public sealed class FaqDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("ticket_id")]
    public string TicketId { get; set; } = string.Empty;

    [BsonElement("ticket_code")]
    public string TicketCode { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("problem_description")]
    public string ProblemDescription { get; set; } = string.Empty;

    [BsonElement("problem_field")]
    public string ProblemField { get; set; } = string.Empty;

    [BsonElement("specific_screen")]
    public string SpecificScreen { get; set; } = string.Empty;

    [BsonElement("bug_type")]
    public string BugType { get; set; } = string.Empty;

    [BsonElement("bug_sub_type")]
    public string BugSubType { get; set; } = string.Empty;

    [BsonElement("resolution_status")]
    [BsonRepresentation(BsonType.String)]
    public FaqResolutionStatus ResolutionStatus { get; set; }

    [BsonElement("resolution_text")]
    public string ResolutionText { get; set; } = string.Empty;

    [BsonElement("severity")]
    [BsonRepresentation(BsonType.String)]
    public Severity Severity { get; set; }

    [BsonElement("impact")]
    [BsonRepresentation(BsonType.String)]
    public ImpactLevel Impact { get; set; }

    [BsonElement("scope")]
    [BsonRepresentation(BsonType.String)]
    public ScopeLevel Scope { get; set; }

    [BsonElement("frequency")]
    [BsonRepresentation(BsonType.String)]
    public FrequencyLevel Frequency { get; set; }

    [BsonElement("urgency")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public UrgencyLevel? Urgency { get; set; }

    [BsonElement("ask_count")]
    public int AskCount { get; set; }

    [BsonElement("requester_external_user_id")]
    public string RequesterExternalUserId { get; set; } = string.Empty;

    [BsonElement("requester_name")]
    public string RequesterName { get; set; } = string.Empty;

    [BsonElement("requester_email")]
    public string RequesterEmail { get; set; } = string.Empty;

    [BsonElement("requester_unit")]
    public string RequesterUnit { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [BsonElement("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public static FaqDocument FromDomain(Faq faq) => new()
    {
        Id = faq.Id,
        TenantId = faq.TenantId.Value,
        Code = faq.Code,
        TicketId = faq.TicketId,
        TicketCode = faq.TicketCode,
        Title = faq.Title,
        ProblemDescription = faq.ProblemDescription,
        ProblemField = faq.Draft.ProblemField,
        SpecificScreen = faq.Draft.SpecificScreen,
        BugType = faq.Draft.BugType,
        BugSubType = faq.Draft.BugSubType,
        ResolutionStatus = faq.ResolutionStatus,
        ResolutionText = faq.ResolutionText,
        Severity = faq.Severity,
        Impact = faq.Impact,
        Scope = faq.Scope,
        Frequency = faq.Frequency,
        Urgency = faq.Urgency,
        AskCount = faq.AskCount,
        RequesterExternalUserId = faq.Requester.ExternalUserId,
        RequesterName = faq.Requester.Name,
        RequesterEmail = faq.Requester.Email,
        RequesterUnit = faq.Requester.Unit,
        CreatedAt = faq.CreatedAt,
        UpdatedAt = faq.UpdatedAt,
    };

    public Faq ToDomain() => new(
        Id,
        DomainTenantId.From(TenantId),
        Code,
        TicketId,
        TicketCode,
        Title,
        ProblemDescription,
        new FaqDraft(ProblemField, SpecificScreen, BugType, BugSubType),
        ResolutionStatus,
        ResolutionText,
        Severity,
        Impact,
        Scope,
        Frequency,
        Urgency,
        AskCount,
        new FaqRequester(RequesterExternalUserId, RequesterName, RequesterEmail, RequesterUnit),
        CreatedAt,
        UpdatedAt);
}

// faq_upvotes: _id composto (tenant:faq:usuário) garante um voto por usuário.
[BsonIgnoreExtraElements]
public sealed class FaqUpvoteDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("faq_id")]
    public string FaqId { get; set; } = string.Empty;

    [BsonElement("user_id")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

// faq_counters: sequência do código FAQ-NN, uma linha por tenant.
[BsonIgnoreExtraElements]
public sealed class FaqCounterDocument
{
    [BsonId]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("seq")]
    public long Seq { get; set; }
}
