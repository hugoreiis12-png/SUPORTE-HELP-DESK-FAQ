using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Domain.Entities;

// AuthorName/AuthorRole vazios (mensagens antigas) são resolvidos na leitura a partir do solicitante do chamado.
public sealed record TicketMessage(
    string Id,
    string TicketId,
    TenantId TenantId,
    string AuthorExternalId,
    string Body,
    MessageVisibility Visibility,
    DateTimeOffset CreatedAt,
    string AuthorName = "",
    string AuthorRole = "");
