namespace Comprove.HelpDesk.Api.Validation;

// Aceita só o nome do nível; Enum.TryParse sozinho aceitaria qualquer número.
public static class LevelName
{
    public static bool IsValid<T>(string? value) where T : struct, Enum =>
        Enum.GetNames<T>().Contains(value, StringComparer.OrdinalIgnoreCase);
}
