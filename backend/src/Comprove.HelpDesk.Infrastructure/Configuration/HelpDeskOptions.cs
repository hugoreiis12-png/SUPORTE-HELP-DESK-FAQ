using System.ComponentModel.DataAnnotations;

namespace Comprove.HelpDesk.Infrastructure.Configuration;

// Segredos (RabbitMq:Password, BubbleSecurity:SigningKey, Comprove:ApiToken) vêm de variável de ambiente ou user-secrets. Nunca no JSON versionado.
public sealed class MongoOptions
{
    public const string Section = "Mongo";

    [Required]
    public string DatabaseName { get; set; } = "helpdesk_database";
}

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMq";

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    [Required]
    public string VirtualHost { get; set; } = "/";

    [Required]
    public string UserName { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class SlaOptions
{
    public const string Section = "Sla";

    // Incrementar ao alterar a matriz: muda a chave de cache (ADR-009).
    [Required]
    public string RulesVersion { get; set; } = "1";
}

public sealed class StorageOptions
{
    public const string Section = "Storage";

    [Range(1, long.MaxValue)]
    public long MaxAttachmentBytes { get; set; } = 25 * 1024 * 1024;

    [Range(1, 60)]
    public int UploadUrlTtlMinutes { get; set; } = 15;

    // URL pública da API, usada para montar os links temporários de anexo.
    [Required]
    public string PublicBaseUrl { get; set; } = string.Empty;

    // Chave HMAC dos links de anexo. Só via env (Storage__LinkSigningKey).
    [Required]
    [MinLength(32)]
    public string LinkSigningKey { get; set; } = string.Empty;
}

public sealed class ComproveOptions : IValidatableObject
{
    public const string Section = "Comprove";

    public string BaseUrl { get; set; } = string.Empty;

    public string? ApiToken { get; set; }

    // Desligado até o endpoint de FAQ ser definido (decisão: depende do backend pronto).
    public bool FaqSyncEnabled { get; set; }

    // A URL do Comprove só é exigida quando a sincronização está ligada (D-12).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FaqSyncEnabled && !Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute))
        {
            yield return new ValidationResult("Comprove:BaseUrl obrigatória com FaqSyncEnabled=true.", [nameof(BaseUrl)]);
        }
    }
}

// Notificação por WhatsApp. Endereço, sessão e chave vêm de variável de ambiente (OpenWa__*).
public sealed class OpenWaOptions : IValidatableObject
{
    public const string Section = "OpenWa";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    // HTTPS obrigatório: a chave e o telefone do usuário trafegam na chamada.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
        {
            yield return new ValidationResult("OpenWa:BaseUrl deve ser uma URL https.", [nameof(BaseUrl)]);
        }

        if (string.IsNullOrWhiteSpace(SessionId) || string.IsNullOrWhiteSpace(ApiKey))
        {
            yield return new ValidationResult("OpenWa:SessionId e OpenWa:ApiKey são obrigatórios.", [nameof(SessionId)]);
        }
    }
}
