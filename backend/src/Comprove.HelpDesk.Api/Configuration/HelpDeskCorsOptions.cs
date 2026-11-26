using System.ComponentModel.DataAnnotations;

namespace Comprove.HelpDesk.Api.Configuration;

// Origens que podem chamar a API pelo navegador (Bubble e localhost de desenvolvimento).
public sealed class HelpDeskCorsOptions
{
    public const string Section = "Cors";
    public const string PolicyName = "HelpDeskFront";

    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = [];
}
