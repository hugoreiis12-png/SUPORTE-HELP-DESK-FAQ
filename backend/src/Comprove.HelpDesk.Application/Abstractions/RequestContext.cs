using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Abstractions;

// Montado pela API a partir do token validado (ou pelo Worker, com ActorId = "system").
public sealed record RequestContext(
    TenantId TenantId,
    string ActorId,
    IReadOnlyCollection<string> Roles,
    string CorrelationId,
    string? ActorName = null,
    string? ActorEmail = null,
    string? ActorUnit = null,
    string? ActorPhone = null)
{
    public const string SupportAgentRole = "support_agent";
    public const string SystemActor = "system";

    public bool IsAgent => Roles.Contains(SupportAgentRole);
}
