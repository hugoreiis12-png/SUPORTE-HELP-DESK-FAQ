using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Api.Mapping;

// Monta o HelpDeskTicket do front. A descrição do problema vira a primeira mensagem do solicitante.
public static class SupportTicketMapper
{
    private const string InitialMessageSuffix = "-initial";

    public static SupportTicketResponse ToResponse(SupportTicketView view)
    {
        var state = view.Ticket.Snapshot();
        var faq = view.Faq;
        return new SupportTicketResponse(
            Id: state.Id,
            TicketCode: state.Protocol.Value,
            LinkedFaqId: faq?.Id,
            LinkedFaqCode: faq?.Code,
            Title: state.Title,
            ProblemDescription: state.Description,
            ProblemField: faq?.Draft.ProblemField,
            SpecificScreen: faq?.Draft.SpecificScreen,
            BugType: faq?.Draft.BugType,
            BugSubType: faq?.Draft.BugSubType,
            Severity: state.Severity?.ToString() ?? faq?.Severity.ToString(),
            SeverityScore: state.SeverityScore ?? faq?.SeverityScore,
            SeverityBreakdown: faq is null ? null : FaqResponseMapper.ToBreakdown(faq),
            Status: FrontendStatusMapper.Map(state.Status),
            RequesterName: faq?.Requester.Name ?? state.RequesterExternalUserId,
            RequesterEmail: faq?.Requester.Email ?? string.Empty,
            RequesterUnit: faq?.Requester.Unit ?? string.Empty,
            AssignedAgent: state.AssignedTo,
            SlaDeadline: state.SlaDeadline,
            CreatedAt: state.CreatedAt,
            UpdatedAt: state.UpdatedAt,
            Image: FaqResponseMapper.ToImage(view.Image),
            ResolutionSummary: ResolutionSummary(faq),
            FaqTargetStatus: faq is null ? null : FrontendFaqStatusMapper.Map(faq.ResolutionStatus),
            Messages: BuildMessages(view, state));
    }

    // O texto padrão de "aguardando triagem" ainda não é uma resolução publicada.
    private static string? ResolutionSummary(Faq? faq) =>
        faq is null || faq.ResolutionText == Faq.PendingResolutionText ? null : faq.ResolutionText;

    private static List<SupportMessageResponse> BuildMessages(SupportTicketView view, TicketSnapshot state)
    {
        var initial = new SupportMessageResponse(
            state.Id + InitialMessageSuffix,
            view.Faq?.Requester.Name ?? state.RequesterExternalUserId,
            MessageAuthorRoles.Requester,
            state.Description,
            state.CreatedAt,
            false);
        var rest = view.Messages.Select(message => ToMessage(message, state.RequesterExternalUserId));
        return [initial, .. rest];
    }

    private static SupportMessageResponse ToMessage(TicketMessage message, string requesterId) => new(
        message.Id,
        string.IsNullOrWhiteSpace(message.AuthorName) ? message.AuthorExternalId : message.AuthorName,
        string.IsNullOrWhiteSpace(message.AuthorRole) ? InferRole(message, requesterId) : message.AuthorRole,
        message.Body,
        message.CreatedAt,
        message.Visibility == MessageVisibility.Internal);

    private static string InferRole(TicketMessage message, string requesterId) =>
        message.AuthorExternalId == requesterId ? MessageAuthorRoles.Requester : MessageAuthorRoles.Support;
}
