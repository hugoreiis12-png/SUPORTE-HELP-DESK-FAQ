using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.Rules;

namespace Comprove.HelpDesk.Application.UseCases;

public sealed record CreateFaqCommand(
    string Title,
    string ProblemDescription,
    FaqDraft Draft,
    ImpactLevel Impact,
    ScopeLevel Scope,
    FrequencyLevel Frequency,
    UrgencyLevel Urgency,
    string IdempotencyKey);

// Registrar uma dúvida abre o chamado do Help Desk e o FAQ vinculado. Idempotente pela chave do chamado:
// se o FAQ falhar depois do chamado, repetir a chamada completa o par sem duplicar.
public sealed class CreateFaqUseCase(OpenTicketUseCase openTicket, IFaqRepository faqs, IClock clock)
{
    public async Task<Faq> ExecuteAsync(RequestContext context, CreateFaqCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            throw new DomainRuleViolationException("idempotency_key obrigatória para registrar o FAQ.");
        }

        FaqTaxonomy.EnsureKnown(command.Draft.ProblemField, command.Draft.BugType);
        var ticket = await openTicket.ExecuteAsync(context, ToTicketCommand(command), ct);
        var ticketId = ticket.Snapshot().Id;

        if (await faqs.FindByTicketIdAsync(context.TenantId, ticketId, ct) is { } existing)
        {
            return existing;
        }

        var number = await faqs.NextNumberAsync(context.TenantId, ct);
        var requester = new FaqRequester(
            context.ActorId,
            context.ActorName ?? context.ActorId,
            context.ActorEmail ?? string.Empty,
            context.ActorUnit ?? string.Empty);
        var faq = Faq.Open(
            Guid.NewGuid().ToString("N"),
            context.TenantId,
            $"FAQ-{number:00}",
            ticket,
            command.Draft,
            requester,
            clock.UtcNow);

        await faqs.InsertAsync(faq, ct);
        return faq;
    }

    // A categoria do chamado é o campo do problema; a classificação detalhada fica no FAQ.
    private static OpenTicketCommand ToTicketCommand(CreateFaqCommand command) => new(
        command.Title,
        command.ProblemDescription,
        command.Draft.ProblemField,
        command.Impact,
        command.Scope,
        command.Frequency,
        command.Urgency,
        command.IdempotencyKey);
}
