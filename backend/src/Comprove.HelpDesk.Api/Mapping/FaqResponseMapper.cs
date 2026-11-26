using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Entities;

namespace Comprove.HelpDesk.Api.Mapping;

public static class FaqResponseMapper
{
    public static FaqResponse ToResponse(FaqView view, bool includeSeverity) =>
        ToResponse(view.Faq, includeSeverity, view.UpvotedByMe, view.Image);

    public static FaqResponse ToResponse(
        Faq faq, bool includeSeverity, bool upvotedByMe = false, AttachmentLink? image = null) => new(
        Id: faq.Id,
        Code: faq.Code,
        Title: faq.Title,
        ProblemDescription: faq.ProblemDescription,
        ProblemField: faq.Draft.ProblemField,
        SpecificScreen: faq.Draft.SpecificScreen,
        BugType: faq.Draft.BugType,
        BugSubType: faq.Draft.BugSubType,
        ResolutionStatus: FrontendFaqStatusMapper.Map(faq.ResolutionStatus),
        ResolutionText: faq.ResolutionText,
        Severity: includeSeverity ? faq.Severity.ToString() : null,
        SeverityScore: includeSeverity ? faq.SeverityScore : null,
        SeverityBreakdown: includeSeverity ? ToBreakdown(faq) : null,
        AskCount: faq.AskCount,
        UpvotedByMe: upvotedByMe,
        TicketId: faq.TicketId,
        TicketCode: faq.TicketCode,
        CreatedAt: faq.CreatedAt,
        UpdatedAt: faq.UpdatedAt,
        Image: ToImage(image));

    public static SeverityBreakdownResponse ToBreakdown(Faq faq) =>
        new(faq.Impact.ToString(), faq.Scope.ToString(), faq.Frequency.ToString(), faq.Urgency?.ToString());

    public static AttachmentLinkResponse? ToImage(AttachmentLink? link) =>
        link is null ? null : new AttachmentLinkResponse(link.AttachmentId, link.ContentType, link.Url.ToString());
}
