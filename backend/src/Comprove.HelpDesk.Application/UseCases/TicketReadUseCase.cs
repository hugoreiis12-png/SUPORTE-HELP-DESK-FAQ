using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.UseCases;

// Leituras (B11). Solicitante vê só os próprios chamados; a listagem é sempre paginada.
public sealed class TicketReadUseCase(ITicketRepository tickets)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Ticket> GetAsync(RequestContext context, string ticketId, CancellationToken ct)
    {
        var ticket = await tickets.FindAsync(context.TenantId, ticketId, ct) ?? throw new TicketNotFoundException(ticketId);
        TicketAccess.EnsureCanSee(context, ticket);
        return ticket;
    }

    public Task<TicketPage> ListAsync(
        RequestContext context,
        TicketStatus? status,
        DateTimeOffset? createdBefore,
        int? pageSize,
        CancellationToken ct)
    {
        var query = new TicketQuery(
            TenantId: context.TenantId,
            Status: status,
            QueueId: null,
            AssignedTo: null,
            RequesterExternalUserId: context.IsAgent ? null : context.ActorId,
            CreatedBefore: createdBefore,
            PageSize: Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
        return tickets.ListAsync(query, ct);
    }
}
