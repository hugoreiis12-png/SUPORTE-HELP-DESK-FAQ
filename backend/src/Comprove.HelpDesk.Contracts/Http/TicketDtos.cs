namespace Comprove.HelpDesk.Contracts.Http;

public sealed record OpenTicketRequest(
    string Title,
    string Description,
    string CategoryId,
    string Impact,
    string Scope,
    string Frequency,
    string Urgency,
    string? IdempotencyKey);

public sealed record UpdateTicketMetaRequest(string CategoryId);

public sealed record AddTicketMessageRequest(string Body, string Visibility);

public sealed record TransitionTicketRequest(string TargetStatus, string? Reason);

public sealed record ResolveTicketRequest(string IdempotencyKey);

public sealed record RequestAttachmentRequest(string FileName, string ContentType, long SizeBytes);

public sealed record AttachmentUploadResponse(string AttachmentId, string UploadUrl, DateTimeOffset ExpiresAt);

// Severidade e critérios de triagem só são preenchidos para agentes de suporte.
public sealed record TicketResponse(
    string Id,
    string TicketCode,
    string Title,
    string Description,
    string CategoryId,
    string? Severity,
    int? SeverityScore,
    string? Impact,
    string? Scope,
    string? Frequency,
    string? Urgency,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SlaDeadline,
    string? AssignedAgent);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, DateTimeOffset? NextCursor);
