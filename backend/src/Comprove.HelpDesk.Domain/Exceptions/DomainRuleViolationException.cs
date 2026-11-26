namespace Comprove.HelpDesk.Domain.Exceptions;

// Violação de regra de negócio. A API mapeia para 422.
public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message)
    {
    }
}
