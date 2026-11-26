using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Domain.Rules;

// Listas fechadas do formulário de FAQ. Fonte única no servidor; o front só renderiza as opções.
public static class FaqTaxonomy
{
    public static readonly IReadOnlySet<string> ProblemFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "Pedidos do Campo",
        "Logística & Rotas",
        "Agendas & Entregas",
        "Canhotos & NF-e",
        "Rastreamento / SAC",
        "App Mobile Offline",
        "Cadastro & Permissões",
        "Importação Excel / Planilhas",
    };

    public static readonly IReadOnlySet<string> BugTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "Falha de Sincronização",
        "Erro de Interface (UI)",
        "Erro de Dados / Cálculo",
        "Lentidão / Performance",
        "Erro de Permissão / Acesso",
        "Falha em Planilha / NF-e",
        "Dúvida Operacional",
    };

    public static void EnsureKnown(string problemField, string bugType)
    {
        if (!ProblemFields.Contains(problemField))
        {
            throw new DomainRuleViolationException($"Campo do problema desconhecido: {problemField}.");
        }

        if (!BugTypes.Contains(bugType))
        {
            throw new DomainRuleViolationException($"Tipo de erro desconhecido: {bugType}.");
        }
    }
}
