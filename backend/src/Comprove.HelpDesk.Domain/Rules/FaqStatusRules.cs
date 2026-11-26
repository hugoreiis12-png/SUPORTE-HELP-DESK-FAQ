using Comprove.HelpDesk.Domain.Enums;

namespace Comprove.HelpDesk.Domain.Rules;

// O status do chamado reflete no FAQ vinculado (regra do Help Desk, espelhada do protótipo).
public static class FaqStatusRules
{
    public static FaqResolutionStatus FromTicket(TicketStatus status) => status switch
    {
        TicketStatus.Resolvido or TicketStatus.Fechado => FaqResolutionStatus.Resolvido,
        TicketStatus.EmAtendimento or TicketStatus.AguardandoSolicitante => FaqResolutionStatus.EmAnalise,
        _ => FaqResolutionStatus.Pendente,
    };
}
