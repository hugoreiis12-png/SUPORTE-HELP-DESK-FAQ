namespace Comprove.HelpDesk.Infrastructure.Persistence.Mongo;

// Nomes de collection do checklist B06. Nenhum nome é digitado fora daqui.
public static class MongoCollectionNames
{
    public const string Tickets = "tickets";
    public const string TicketHistory = "ticket_history";
    public const string TicketComments = "ticket_comments";
    public const string TicketAttachments = "ticket_attachments";
    public const string TicketCategories = "ticket_categories";
    public const string SupportQueues = "support_queues";
    public const string SupportTeams = "support_teams";
    public const string SlaPolicies = "sla_policies";
    public const string OutboxEvents = "outbox_events";
    public const string ProcessedMessages = "processed_messages";
    public const string ProtocolSequences = "protocol_sequences";
    public const string Faqs = "faqs";
    public const string FaqUpvotes = "faq_upvotes";
    public const string FaqCounters = "faq_counters";
}
