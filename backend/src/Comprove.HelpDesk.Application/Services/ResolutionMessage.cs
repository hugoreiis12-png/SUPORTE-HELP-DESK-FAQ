using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Application.Services;

// Texto enviado ao solicitante. Usa só a resolução oficial do FAQ; mensagens e notas internas nunca entram.
public static class ResolutionMessage
{
    private const int MaxLength = 4096;
    private const string Ellipsis = "…";

    public static string Compose(TicketSnapshot ticket, Faq? faq)
    {
        var greeting = string.IsNullOrWhiteSpace(ticket.Requester?.Name) ? "Olá!" : $"Olá, {ticket.Requester.Name.Trim()}!";
        var text = $"{greeting} Seu chamado {ticket.Protocol.Value} — \"{ticket.Title}\" — foi resolvido.";
        if (OfficialResolution(faq) is { } resolution)
        {
            text += $"\n\nResolução: {resolution}";
        }

        return text.Length <= MaxLength ? text : text[..(MaxLength - Ellipsis.Length)] + Ellipsis;
    }

    private static string? OfficialResolution(Faq? faq) =>
        faq is null
        || faq.ResolutionStatus != FaqResolutionStatus.Resolvido
        || faq.ResolutionText == Faq.PendingResolutionText
            ? null
            : faq.ResolutionText;
}
