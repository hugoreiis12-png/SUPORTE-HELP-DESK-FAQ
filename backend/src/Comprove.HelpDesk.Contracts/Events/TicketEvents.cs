using System.Text.Json.Serialization;

namespace Comprove.HelpDesk.Contracts.Events;

public static class TicketEventTypes
{
    public const string Created = "helpdesk.ticket.created";
    public const string Updated = "helpdesk.ticket.updated";
    public const string StatusChanged = "helpdesk.ticket.status-changed";
    public const string Resolved = "helpdesk.ticket.resolved";
    public const string CommentCreated = "helpdesk.comment.created";
    public const string IntegrationQuery = "helpdesk.integration.comprove.query";
}

public sealed record TicketCreatedPayload(
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("category_id")] string CategoryId);

public sealed record TicketStatusChangedPayload(
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("from_status")] string FromStatus,
    [property: JsonPropertyName("to_status")] string ToStatus);

public sealed record TicketResolvedPayload(
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("resolved_at")] DateTimeOffset ResolvedAt);

public sealed record IntegrationQueryPayload(
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("query_kind")] string QueryKind);

public sealed record TicketUpdatedPayload(
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("category_id")] string CategoryId);

public sealed record CommentCreatedPayload(
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("message_id")] string MessageId);
