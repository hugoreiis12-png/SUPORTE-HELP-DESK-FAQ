using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Domain.Rules;

// Matriz inicial (ADR-008): Crítica 2h, Alta 6h, Média 24h, Baixa 48h.
public sealed class SlaPolicy
{
    private readonly IReadOnlyDictionary<Severity, TimeSpan> _targets;

    public SlaPolicy(IReadOnlyDictionary<Severity, TimeSpan> targets)
    {
        _targets = targets;
    }

    public static SlaPolicy Default { get; } = new(new Dictionary<Severity, TimeSpan>
    {
        [Severity.Crítica] = TimeSpan.FromHours(2),
        [Severity.Alta] = TimeSpan.FromHours(6),
        [Severity.Média] = TimeSpan.FromHours(24),
        [Severity.Baixa] = TimeSpan.FromHours(48),
    });

    public TimeSpan TargetFor(Severity severity) =>
        _targets.TryGetValue(severity, out var target)
            ? target
            : throw new DomainRuleViolationException($"SLA não configurado para {severity}.");

    public DateTimeOffset DeadlineFrom(DateTimeOffset openedAt, Severity severity) =>
        openedAt + TargetFor(severity);
}
