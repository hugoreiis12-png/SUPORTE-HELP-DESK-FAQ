using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Domain.Rules;

// Classificação determinística (ADR-012): soma de 20 a 100 pontos. Sem IA nesta fase.
public static class SeverityClassifier
{
    private const int CriticalThreshold = 90;
    private const int HighThreshold = 70;
    private const int MediumThreshold = 50;

    // Chamados anteriores à pergunta de urgência não a têm; contam o peso intermediário.
    private const UrgencyLevel LegacyUrgency = UrgencyLevel.Media;

    public static int Score(ScopeLevel scope, ImpactLevel impact, FrequencyLevel frequency, UrgencyLevel urgency) =>
        (int)scope + (int)impact + (int)frequency + (int)urgency;

    public static Severity Classify(ScopeLevel scope, ImpactLevel impact, FrequencyLevel frequency, UrgencyLevel? urgency) =>
        ComputeSeverity(Score(scope, impact, frequency, urgency ?? LegacyUrgency));

    public static Severity ComputeSeverity(int total) => total switch
    {
        >= CriticalThreshold => Severity.Crítica,
        >= HighThreshold => Severity.Alta,
        >= MediumThreshold => Severity.Média,
        _ => Severity.Baixa,
    };
}
