using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Rules;
using FluentValidation;

namespace Comprove.HelpDesk.Api.Validation;

// Validação de borda do registro de dúvida. A taxonomia vem do Domain (fonte única).
public sealed class CreateFaqRequestValidator : AbstractValidator<CreateFaqRequest>
{
    public CreateFaqRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(Ticket.MaxTitleLength);
        RuleFor(request => request.ProblemDescription).NotEmpty().MaximumLength(Ticket.MaxDescriptionLength);
        RuleFor(request => request.ProblemField).Must(FaqTaxonomy.ProblemFields.Contains).WithMessage("Campo do problema desconhecido.");
        RuleFor(request => request.BugType).Must(FaqTaxonomy.BugTypes.Contains).WithMessage("Tipo de erro desconhecido.");
        RuleFor(request => request.SpecificScreen).NotEmpty().MaximumLength(Faq.MaxFieldLength);
        RuleFor(request => request.BugSubType).NotEmpty().MaximumLength(Faq.MaxFieldLength);
        RuleFor(request => request.IdempotencyKey).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Impact).Must(LevelName.IsValid<ImpactLevel>).WithMessage("Impacto inválido.");
        RuleFor(request => request.Scope).Must(LevelName.IsValid<ScopeLevel>).WithMessage("Escopo inválido.");
        RuleFor(request => request.Frequency).Must(LevelName.IsValid<FrequencyLevel>).WithMessage("Frequência inválida.");
        RuleFor(request => request.Urgency).Must(LevelName.IsValid<UrgencyLevel>).WithMessage("Urgência inválida.");
    }
}
