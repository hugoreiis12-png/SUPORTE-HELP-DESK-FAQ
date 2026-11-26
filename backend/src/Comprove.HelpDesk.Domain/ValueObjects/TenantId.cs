using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Domain.ValueObjects;

// Sempre derivado do token validado (ADR-006), nunca do corpo da requisição.
public readonly record struct TenantId
{
    private TenantId(string value) => Value = value;

    public string Value { get; }

    public static TenantId From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException("tenant_id obrigatório.");
        }

        return new TenantId(value.Trim());
    }

    public override string ToString() => Value;
}
