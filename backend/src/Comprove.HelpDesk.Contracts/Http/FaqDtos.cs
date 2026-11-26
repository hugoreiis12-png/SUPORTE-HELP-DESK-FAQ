namespace Comprove.HelpDesk.Contracts.Http;

public sealed record CreateFaqRequest(
    string Title,
    string ProblemDescription,
    string ProblemField,
    string SpecificScreen,
    string BugType,
    string BugSubType,
    string Impact,
    string Scope,
    string Frequency,
    string Urgency,
    string IdempotencyKey);

public sealed record SeverityBreakdownResponse(string Impact, string Scope, string Frequency, string? Urgency);

public sealed record AttachmentLinkResponse(string AttachmentId, string ContentType, string Url);

// Espelha FaqItem do front. Os campos de severidade só são preenchidos para agentes de suporte.
public sealed record FaqResponse(
    string Id,
    string Code,
    string Title,
    string ProblemDescription,
    string ProblemField,
    string SpecificScreen,
    string BugType,
    string BugSubType,
    string ResolutionStatus,
    string ResolutionText,
    string? Severity,
    int? SeverityScore,
    SeverityBreakdownResponse? SeverityBreakdown,
    int AskCount,
    bool UpvotedByMe,
    string TicketId,
    string TicketCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    AttachmentLinkResponse? Image);

public sealed record UpvoteResponse(int AskCount, bool Upvoted);
