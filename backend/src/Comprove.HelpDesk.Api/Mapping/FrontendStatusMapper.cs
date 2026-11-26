using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;

namespace Comprove.HelpDesk.Api.Mapping;

// Mapeamento explícito do checklist B21: rótulos do protótipo React, não estados internos.
public static class FrontendStatusMapper
{
    private const string New = "Novo";
    private const string InProgress = "Em Atendimento";
    private const string WaitingRequester = "Aguardando Solicitante";
    private const string Resolved = "Resolvido";

    // Destino manual vindo do front. "Novo" não é destino: o Worker leva o chamado até lá (triagem/despacho).
    public static TicketStatus ParseTarget(string value) => value.Trim() switch
    {
        InProgress => TicketStatus.EmAtendimento,
        WaitingRequester => TicketStatus.AguardandoSolicitante,
        Resolved => TicketStatus.Resolvido,
        New => throw new DomainRuleViolationException("\"Novo\" não pode ser escolhido manualmente."),
        var other when Enum.TryParse<TicketStatus>(other, ignoreCase: true, out var parsed) => parsed,
        var other => throw new DomainRuleViolationException($"Status inválido: {other}."),
    };

    public static string Map(TicketStatus status) => status switch
    {
        TicketStatus.Recebido => New,
        TicketStatus.EmTriagem => New,
        TicketStatus.AguardandoAtendimento => New,
        TicketStatus.EmAtendimento => InProgress,
        TicketStatus.AguardandoSolicitante => WaitingRequester,
        TicketStatus.Resolvido => Resolved,
        TicketStatus.Fechado => Resolved,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status sem rótulo de frontend."),
    };
}
