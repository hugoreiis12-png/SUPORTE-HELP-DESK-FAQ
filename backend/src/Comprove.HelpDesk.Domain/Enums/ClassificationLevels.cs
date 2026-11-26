namespace Comprove.HelpDesk.Domain.Enums;

// Pontuação das quatro perguntas de triagem (ADR-012). O valor do enum é o peso.
public enum ScopeLevel
{
    Alto = 25,
    Medio = 15,
    Baixo = 5
}

public enum ImpactLevel
{
    Alto = 25,
    Medio = 15,
    Baixo = 5
}

public enum FrequencyLevel
{
    Alta = 25,
    Media = 15,
    Baixa = 5
}

public enum UrgencyLevel
{
    Alta = 25,
    Media = 15,
    Baixa = 5
}
