using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.Rules;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Domain.Entities;

public sealed record FaqDraft(string ProblemField, string SpecificScreen, string BugType, string BugSubType);

public sealed record FaqRequester(string ExternalUserId, string Name, string Email, string Unit);

// FAQ nasce junto do chamado (1:1) e guarda a classificação que o Help Desk exibe.
public sealed record Faq(
    string Id,
    TenantId TenantId,
    string Code,
    string TicketId,
    string TicketCode,
    string Title,
    string ProblemDescription,
    FaqDraft Draft,
    FaqResolutionStatus ResolutionStatus,
    string ResolutionText,
    Severity Severity,
    ImpactLevel Impact,
    ScopeLevel Scope,
    FrequencyLevel Frequency,
    UrgencyLevel? Urgency,
    int AskCount,
    FaqRequester Requester,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public const int MaxFieldLength = 120;
    public const int MaxResolutionLength = 4000;
    public const string PendingResolutionText =
        "Solicitação registrada e encaminhada ao suporte Help Desk. Aguardando triagem e definição oficial do suporte.";

    public int? SeverityScore => Urgency is null
        ? null
        : SeverityClassifier.Score(Scope, Impact, Frequency, Urgency.Value);

    public static Faq Open(
        string id,
        TenantId tenant,
        string code,
        Ticket ticket,
        FaqDraft draft,
        FaqRequester requester,
        DateTimeOffset now)
    {
        FaqTaxonomy.EnsureKnown(draft.ProblemField, draft.BugType);
        EnsureField(draft.SpecificScreen, "Tela específica");
        EnsureField(draft.BugSubType, "Subtipo do erro");

        var state = ticket.Snapshot();
        return new Faq(
            id,
            tenant,
            code,
            state.Id,
            state.Protocol.Value,
            state.Title,
            state.Description,
            draft,
            FaqResolutionStatus.Pendente,
            PendingResolutionText,
            SeverityClassifier.Classify(state.Scope, state.Impact, state.Frequency, state.Urgency),
            state.Impact,
            state.Scope,
            state.Frequency,
            state.Urgency,
            AskCount: 1,
            requester,
            now,
            now);
    }

    public Faq WithStatus(FaqResolutionStatus status, DateTimeOffset now) =>
        this with { ResolutionStatus = status, UpdatedAt = now };

    public Faq WithResolution(FaqResolutionStatus status, string text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > MaxResolutionLength)
        {
            throw new DomainRuleViolationException($"Resolução obrigatória com até {MaxResolutionLength} caracteres.");
        }

        return this with { ResolutionStatus = status, ResolutionText = text.Trim(), UpdatedAt = now };
    }

    private static void EnsureField(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > MaxFieldLength)
        {
            throw new DomainRuleViolationException($"{label} obrigatório com até {MaxFieldLength} caracteres.");
        }
    }
}
