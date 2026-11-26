using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Rules;
using Xunit;

namespace Comprove.HelpDesk.Tests.Unit.Domain;

public sealed class SlaPolicyAndClassifierTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(Severity.Crítica, 2)]
    [InlineData(Severity.Alta, 6)]
    [InlineData(Severity.Média, 24)]
    [InlineData(Severity.Baixa, 48)]
    public void Default_sla_matches_adr_008(Severity severity, int expectedHours)
    {
        Assert.Equal(TimeSpan.FromHours(expectedHours), SlaPolicy.Default.TargetFor(severity));
        Assert.Equal(OpenedAt.AddHours(expectedHours), SlaPolicy.Default.DeadlineFrom(OpenedAt, severity));
    }

    [Theory]
    [InlineData(ScopeLevel.Alto, ImpactLevel.Alto, FrequencyLevel.Alta, UrgencyLevel.Alta, 100, Severity.Crítica)]
    [InlineData(ScopeLevel.Alto, ImpactLevel.Alto, FrequencyLevel.Alta, UrgencyLevel.Media, 90, Severity.Crítica)]
    [InlineData(ScopeLevel.Medio, ImpactLevel.Medio, FrequencyLevel.Media, UrgencyLevel.Media, 60, Severity.Média)]
    [InlineData(ScopeLevel.Baixo, ImpactLevel.Baixo, FrequencyLevel.Baixa, UrgencyLevel.Baixa, 20, Severity.Baixa)]
    public void Classifies_by_the_sum_of_the_four_answers(
        ScopeLevel scope, ImpactLevel impact, FrequencyLevel frequency, UrgencyLevel urgency, int points, Severity expected)
    {
        Assert.Equal(points, SeverityClassifier.Score(scope, impact, frequency, urgency));
        Assert.Equal(expected, SeverityClassifier.Classify(scope, impact, frequency, urgency));
    }

    [Theory]
    [InlineData(100, Severity.Crítica)]
    [InlineData(90, Severity.Crítica)]
    [InlineData(80, Severity.Alta)]
    [InlineData(70, Severity.Alta)]
    [InlineData(60, Severity.Média)]
    [InlineData(50, Severity.Média)]
    [InlineData(40, Severity.Baixa)]
    [InlineData(20, Severity.Baixa)]
    public void Thresholds_match_adr_012(int total, Severity expected)
    {
        Assert.Equal(expected, SeverityClassifier.ComputeSeverity(total));
    }

    [Fact]
    public void Missing_urgency_counts_as_the_middle_weight()
    {
        var legacy = SeverityClassifier.Classify(ScopeLevel.Alto, ImpactLevel.Alto, FrequencyLevel.Media, null);

        Assert.Equal(Severity.Alta, legacy);
    }
}
