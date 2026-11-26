using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Rules;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Domain.Entities;

// Estado imutável do agregado. Persistência mapeia daqui e para cá; o domínio nunca conhece o Mongo.
public sealed record TicketSnapshot(
    string Id,
    TenantId TenantId,
    ProtocolNumber Protocol,
    string RequesterExternalUserId,
    string CategoryId,
    string Title,
    string Description,
    ImpactLevel Impact,
    ScopeLevel Scope,
    FrequencyLevel Frequency,
    UrgencyLevel? Urgency,
    Severity? Severity,
    TicketStatus Status,
    string? QueueId,
    string? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SlaDeadline,
    DateTimeOffset? PausedAt,
    DateTimeOffset? ResolvedAt,
    long Version,
    string? IdempotencyKey,
    RequesterProfile? Requester = null)
{
    // Pontos só existem depois da classificação e quando as quatro respostas foram registradas.
    public int? SeverityScore => Severity is null || Urgency is null
        ? null
        : SeverityClassifier.Score(Scope, Impact, Frequency, Urgency.Value);
}
