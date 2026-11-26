using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Application.Queries;

public sealed record FaqQuery(
    TenantId TenantId,
    FaqResolutionStatus? Status,
    string? Search,
    DateTimeOffset? CreatedBefore,
    int PageSize);

public sealed record Page<T>(IReadOnlyList<T> Items, DateTimeOffset? NextCursor);

public sealed record FaqUpvote(int AskCount, bool Upvoted);

// Link temporário (assinado) para o anexo de imagem do chamado.
public sealed record AttachmentLink(string AttachmentId, string ContentType, Uri Url);

public sealed record FaqView(Faq Faq, bool UpvotedByMe, AttachmentLink? Image);

public sealed record SupportTicketView(
    Ticket Ticket,
    Faq? Faq,
    IReadOnlyList<TicketMessage> Messages,
    AttachmentLink? Image);
