namespace Comprove.HelpDesk.Application.Services;

// Só imagem sem script: SVG e HTML ficam fora porque executariam no domínio da API.
public static class AttachmentContentTypes
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
    };

    public static bool IsAllowed(string? contentType) => contentType is not null && Allowed.Contains(contentType);
}
