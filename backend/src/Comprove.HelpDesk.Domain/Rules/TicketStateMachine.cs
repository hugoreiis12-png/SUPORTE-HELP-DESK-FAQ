using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Domain.Rules;

// Transições válidas (B05/D-09). Recebido→Resolvido e Fechado→EmAtendimento são proibidas.
public static class TicketStateMachine
{
    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> Allowed =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.Recebido] = [TicketStatus.EmTriagem],
            [TicketStatus.EmTriagem] = [TicketStatus.AguardandoAtendimento],
            [TicketStatus.AguardandoAtendimento] = [TicketStatus.EmAtendimento],
            [TicketStatus.EmAtendimento] = [TicketStatus.AguardandoSolicitante, TicketStatus.Resolvido],
            [TicketStatus.AguardandoSolicitante] = [TicketStatus.EmAtendimento],
            [TicketStatus.Resolvido] = [TicketStatus.Fechado, TicketStatus.EmAtendimento],
            [TicketStatus.Fechado] = [],
        };

    public static bool IsReopen(TicketStatus from, TicketStatus to) =>
        from == TicketStatus.Resolvido && to == TicketStatus.EmAtendimento;

    public static bool CanTransition(TicketStatus from, TicketStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static void EnsureAllowed(TicketStatus from, TicketStatus to, string? reason)
    {
        if (!CanTransition(from, to))
        {
            throw new DomainRuleViolationException($"Transição inválida: {from} → {to}.");
        }

        if (IsReopen(from, to) && string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("Reabertura exige motivo.");
        }
    }
}
