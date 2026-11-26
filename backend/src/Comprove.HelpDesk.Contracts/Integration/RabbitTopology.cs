namespace Comprove.HelpDesk.Contracts.Integration;

public static class RabbitTopology
{
    public const string TopicExchange = "helpdesk.topic";
    public const string DeadLetterExchange = "helpdesk.dlx";

    public const string TicketCreatedRoutingKey = "helpdesk.ticket.created";
    public const string TicketUpdatedRoutingKey = "helpdesk.ticket.updated";
    public const string TicketStatusChangedRoutingKey = "helpdesk.ticket.status-changed";
    public const string TicketResolvedRoutingKey = "helpdesk.ticket.resolved";
    public const string CommentCreatedRoutingKey = "helpdesk.comment.created";
    public const string IntegrationQueryRoutingKey = "helpdesk.integration.comprove.query";

    public const string ClassificationQueue = "ticket.created.classification";
    public const string DispatchQueue = "ticket.classified.dispatch";
    public const string ResolvedNotifyQueue = "ticket.resolved.notify";
    public const string IntegrationQueue = "integration.comprove.query";

    private static readonly HashSet<string> ConsumedRoutingKeys =
    [
        TicketCreatedRoutingKey,
        TicketUpdatedRoutingKey,
        TicketStatusChangedRoutingKey,
        TicketResolvedRoutingKey,
        IntegrationQueryRoutingKey,
    ];

    // Evento sem fila ligada (ex.: comentário) não exige destino; os demais precisam chegar a uma fila.
    public static bool HasConsumer(string routingKey) => ConsumedRoutingKeys.Contains(routingKey);

    public static string DeadLetterQueueOf(string queue) => $"{queue}.dlq";
}
