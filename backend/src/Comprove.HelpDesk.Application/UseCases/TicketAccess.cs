using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Domain.Entities;

namespace Comprove.HelpDesk.Application.UseCases;

// Regra de autorização centralizada (B09/B11). Chamada em todo caso de uso, não só na API.
public static class TicketAccess
{
    public static void RequireAgent(RequestContext context)
    {
        if (!context.IsAgent)
        {
            throw new TicketAccessDeniedException();
        }
    }

    public static void EnsureCanSee(RequestContext context, Ticket ticket)
    {
        if (context.IsAgent)
        {
            return;
        }

        if (ticket.Snapshot().RequesterExternalUserId != context.ActorId)
        {
            throw new TicketAccessDeniedException();
        }
    }
}
