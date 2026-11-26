namespace Comprove.HelpDesk.Contracts.Http;

public sealed record PublishResolutionRequest(string ResolutionSummary, string FaqStatus, string IdempotencyKey);

// authorRole: solicitante | suporte | sistema (vocabulário do front).
public sealed record SupportMessageResponse(
    string Id,
    string AuthorName,
    string AuthorRole,
    string Message,
    DateTimeOffset Timestamp,
    bool IsInternalNote);

// Espelha HelpDeskTicket do front. Os dados de classificação vêm do FAQ vinculado.
public sealed record SupportTicketResponse(
    string Id,
    string TicketCode,
    string? LinkedFaqId,
    string? LinkedFaqCode,
    string Title,
    string ProblemDescription,
    string? ProblemField,
    string? SpecificScreen,
    string? BugType,
    string? BugSubType,
    string? Severity,
    int? SeverityScore,
    SeverityBreakdownResponse? SeverityBreakdown,
    string Status,
    string RequesterName,
    string RequesterEmail,
    string RequesterUnit,
    string? AssignedAgent,
    DateTimeOffset? SlaDeadline,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    AttachmentLinkResponse? Image,
    string? ResolutionSummary,
    string? FaqTargetStatus,
    IReadOnlyList<SupportMessageResponse> Messages);

public sealed record MeResponse(
    string UserId,
    string TenantId,
    string? Name,
    string? Email,
    string? Unit,
    IReadOnlyList<string> Roles,
    bool IsSupportAgent);
