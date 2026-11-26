using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Queries;

public sealed record TicketQuery(
    TenantId TenantId,
    TicketStatus? Status,
    string? QueueId,
    string? AssignedTo,
    string? RequesterExternalUserId,
    DateTimeOffset? CreatedBefore,
    int PageSize);

public sealed record TicketPage(IReadOnlyList<Ticket> Items, DateTimeOffset? NextCursor);
