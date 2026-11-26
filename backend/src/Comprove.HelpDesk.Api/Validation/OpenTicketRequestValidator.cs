using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using FluentValidation;

namespace Comprove.HelpDesk.Api.Validation;

// Validação de borda (uma vez, na API). Regras de domínio ficam no Domain.
public sealed class OpenTicketRequestValidator : AbstractValidator<OpenTicketRequest>
{
    public OpenTicketRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(Ticket.MaxTitleLength);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(Ticket.MaxDescriptionLength);
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.Impact).Must(LevelName.IsValid<ImpactLevel>).WithMessage("Impacto inválido.");
        RuleFor(request => request.Scope).Must(LevelName.IsValid<ScopeLevel>).WithMessage("Escopo inválido.");
        RuleFor(request => request.Frequency).Must(LevelName.IsValid<FrequencyLevel>).WithMessage("Frequência inválida.");
        RuleFor(request => request.Urgency).Must(LevelName.IsValid<UrgencyLevel>).WithMessage("Urgência inválida.");
    }
}
