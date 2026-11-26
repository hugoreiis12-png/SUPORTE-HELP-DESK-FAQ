namespace Comprove.HelpDesk.Application.Abstractions;

public sealed record TeamAssignment(string QueueId, string? AgentId);

public interface ITeamRoster
{
    // Fila pela categoria (ou a fila padrão) e agente por rodízio auditável (B17).
    Task<TeamAssignment> NextAssignmentAsync(string categoryId, CancellationToken ct);
}
