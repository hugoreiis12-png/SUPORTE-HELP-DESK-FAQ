using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Entities;

namespace Comprove.HelpDesk.Api.Mapping;

public static class TicketResponseMapper
{
    public static TicketResponse ToResponse(Ticket ticket, bool includeSeverity)
    {
        var state = ticket.Snapshot();
        return new TicketResponse(
            Id: state.Id,
            TicketCode: state.Protocol.Value,
            Title: state.Title,
            Description: state.Description,
            CategoryId: state.CategoryId,
            Severity: includeSeverity ? state.Severity?.ToString() : null,
            SeverityScore: includeSeverity ? state.SeverityScore : null,
            Impact: includeSeverity ? state.Impact.ToString() : null,
            Scope: includeSeverity ? state.Scope.ToString() : null,
            Frequency: includeSeverity ? state.Frequency.ToString() : null,
            Urgency: includeSeverity ? state.Urgency?.ToString() : null,
            Status: FrontendStatusMapper.Map(state.Status),
            CreatedAt: state.CreatedAt,
            UpdatedAt: state.UpdatedAt,
            SlaDeadline: state.SlaDeadline,
            AssignedAgent: state.AssignedTo);
    }
}
