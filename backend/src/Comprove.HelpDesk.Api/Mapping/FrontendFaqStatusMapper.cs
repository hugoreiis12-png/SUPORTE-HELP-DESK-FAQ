using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Api.Mapping;

// Rótulos do protótipo React para o status de resolução do FAQ (e a volta, aceitando também o nome do enum).
public static class FrontendFaqStatusMapper
{
    public static string Map(FaqResolutionStatus status) => status switch
    {
        FaqResolutionStatus.Pendente => "Pendente",
        FaqResolutionStatus.EmAnalise => "Em Análise",
        FaqResolutionStatus.SolucaoDeContorno => "Solução de Contorno",
        FaqResolutionStatus.Resolvido => "Resolvido",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status de FAQ sem rótulo."),
    };

    public static FaqResolutionStatus Parse(string value) => value.Trim() switch
    {
        "Pendente" => FaqResolutionStatus.Pendente,
        "Em Análise" => FaqResolutionStatus.EmAnalise,
        "Solução de Contorno" => FaqResolutionStatus.SolucaoDeContorno,
        "Resolvido" => FaqResolutionStatus.Resolvido,
        var other when Enum.TryParse<FaqResolutionStatus>(other, ignoreCase: true, out var parsed) => parsed,
        var other => throw new DomainRuleViolationException($"Status de FAQ inválido: {other}."),
    };
}
