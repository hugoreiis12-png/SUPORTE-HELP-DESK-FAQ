namespace Comprove.HelpDesk.Domain.ValueObjects;

// Celular em formato internacional só com dígitos (ex.: 5521999999999), como o gateway de mensagens espera.
public static class PhoneNumber
{
    private const string BrazilCode = "55";
    private const string Separators = " ()-.";
    private const int MinInternationalDigits = 12;
    private const int MinExplicitDigits = 10;
    private const int MaxDigits = 15;

    // Número inválido vira null: o chamado é aberto normalmente, só não haverá notificação.
    public static string? Normalize(string? raw)
    {
        var text = raw?.Trim() ?? string.Empty;
        var isExplicit = text.StartsWith('+');
        var body = isExplicit ? text[1..] : text;
        if (body.Any(character => !char.IsAsciiDigit(character) && !Separators.Contains(character)))
        {
            return null;
        }

        var digits = new string(body.Where(char.IsAsciiDigit).ToArray());
        // Sem "+", 10 ou 11 dígitos são DDD + número do Brasil.
        if (!isExplicit && digits.Length is 10 or 11)
        {
            return BrazilCode + digits;
        }

        var minimum = isExplicit ? MinExplicitDigits : MinInternationalDigits;
        return digits.Length >= minimum && digits.Length <= MaxDigits ? digits : null;
    }
}
