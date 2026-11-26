using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Domain.Entities;

public sealed record StatusChange(TicketStatus From, TicketStatus To, string? Reason);
