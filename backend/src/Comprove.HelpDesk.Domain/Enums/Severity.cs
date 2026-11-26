namespace Comprove.HelpDesk.Domain.Enums;

// O valor do enum é o peso. SLA: Crítica 2h, Alta 6h, Média 24h, Baixa 48h (ADR-008).
public enum Severity
{
    Crítica = 4,
    Alta = 3,
    Média = 2,
    Baixa = 1
}
