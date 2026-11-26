using System.ComponentModel.DataAnnotations;

namespace Comprove.HelpDesk.Api.Configuration;

// SigningKey obrigatório via env (BubbleSecurity__SigningKey) ou user-secrets. Nunca no JSON versionado.
public sealed class BubbleSecurityOptions
{
    public const string Section = "BubbleSecurity";

    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(60, 3600)]
    public int TokenTtlSeconds { get; set; } = 300;
}
