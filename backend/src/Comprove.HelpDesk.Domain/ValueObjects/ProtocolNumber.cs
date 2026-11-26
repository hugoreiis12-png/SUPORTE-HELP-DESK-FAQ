using System.Text.RegularExpressions;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Domain.ValueObjects;

// Protocolo global SUP-YYYY-NNNNNN (D-08).
public sealed partial class ProtocolNumber
{
    private const string Prefix = "SUP";
    private const int MinYear = 2000;
    private const int SequenceWidth = 6;

    private ProtocolNumber(string value) => Value = value;

    public string Value { get; }

    public static ProtocolNumber Create(int year, long sequence)
    {
        if (year < MinYear || sequence <= 0)
        {
            throw new DomainRuleViolationException("Protocolo inválido: ano ou sequência fora do intervalo.");
        }

        return new ProtocolNumber($"{Prefix}-{year:D4}-{sequence.ToString($"D{SequenceWidth}")}");
    }

    public static ProtocolNumber Parse(string value)
    {
        if (!ProtocolPattern().IsMatch(value))
        {
            throw new DomainRuleViolationException("Protocolo em formato inválido.");
        }

        return new ProtocolNumber(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^SUP-\d{4}-\d{6,}$")]
    private static partial Regex ProtocolPattern();
}
