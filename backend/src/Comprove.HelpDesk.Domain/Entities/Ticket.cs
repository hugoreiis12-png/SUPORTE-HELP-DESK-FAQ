using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.Rules;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Domain.Entities;

// Agregado Ticket. Mutações substituem o snapshot inteiro (imutável) e incrementam Version.
public sealed class Ticket
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 4000;

    private TicketSnapshot _state;

    public Ticket(TicketSnapshot state) => _state = state;

    public TicketSnapshot Snapshot() => _state;

    public static Ticket Open(
        string id,
        TenantId tenant,
        ProtocolNumber protocol,
        string requesterExternalUserId,
        string categoryId,
        string title,
        string description,
        ImpactLevel impact,
        ScopeLevel scope,
        FrequencyLevel frequency,
        UrgencyLevel urgency,
        DateTimeOffset now,
        string? idempotencyKey,
        RequesterProfile? requester = null)
    {
        EnsureNotBlank(requesterExternalUserId, "Solicitante obrigatório.");
        EnsureNotBlank(categoryId, "Categoria obrigatória.");
        EnsureText(title, MaxTitleLength, "Título");
        EnsureText(description, MaxDescriptionLength, "Descrição");

        return new Ticket(new TicketSnapshot(
            Id: id,
            TenantId: tenant,
            Protocol: protocol,
            RequesterExternalUserId: requesterExternalUserId,
            CategoryId: categoryId,
            Title: title.Trim(),
            Description: description.Trim(),
            Impact: impact,
            Scope: scope,
            Frequency: frequency,
            Urgency: urgency,
            Severity: null,
            Status: TicketStatus.Recebido,
            QueueId: null,
            AssignedTo: null,
            CreatedAt: now,
            UpdatedAt: now,
            SlaDeadline: null,
            PausedAt: null,
            ResolvedAt: null,
            Version: 1,
            IdempotencyKey: idempotencyKey,
            Requester: requester));
    }

    // Recebido → EmTriagem, com severidade e SLA calculados a partir da abertura (ADR-012/ADR-008).
    public StatusChange Classify(SlaPolicy sla, DateTimeOffset now)
    {
        if (_state.Status != TicketStatus.Recebido)
        {
            throw new DomainRuleViolationException("Só chamados recebidos podem ser classificados.");
        }

        var severity = SeverityClassifier.Classify(_state.Scope, _state.Impact, _state.Frequency, _state.Urgency);
        var change = Transition(TicketStatus.EmTriagem, now, null);
        _state = _state with
        {
            Severity = severity,
            SlaDeadline = sla.DeadlineFrom(_state.CreatedAt, severity),
        };
        return change;
    }

    // EmTriagem → AguardandoAtendimento; se houver agente, → EmAtendimento.
    public IReadOnlyList<StatusChange> Dispatch(string queueId, string? agentId, DateTimeOffset now)
    {
        EnsureNotBlank(queueId, "Fila obrigatória.");
        var queued = Transition(TicketStatus.AguardandoAtendimento, now, null);
        _state = _state with { QueueId = queueId, AssignedTo = agentId };

        if (agentId is null)
        {
            return [queued];
        }

        return [queued, Transition(TicketStatus.EmAtendimento, now, "atribuído")];
    }

    public StatusChange Transition(TicketStatus to, DateTimeOffset now, string? reason)
    {
        var from = _state.Status;
        TicketStateMachine.EnsureAllowed(from, to, reason);

        var next = _state with { Status = to, UpdatedAt = now, Version = _state.Version + 1 };
        next = ApplySla(next, from, to, now);
        _state = ApplyResolution(next, from, to, now);
        return new StatusChange(from, to, reason);
    }

    public void ChangeCategory(string categoryId, DateTimeOffset now)
    {
        EnsureNotBlank(categoryId, "Categoria obrigatória.");
        EnsureNotClosed();
        _state = _state with { CategoryId = categoryId, UpdatedAt = now, Version = _state.Version + 1 };
    }

    // Publicação sem troca de status também incrementa Version, para barrar gravações simultâneas.
    public void RegisterActivity(DateTimeOffset now)
    {
        EnsureNotClosed();
        _state = _state with { UpdatedAt = now, Version = _state.Version + 1 };
    }

    public void EnsureCanReceiveMessages()
    {
        EnsureNotClosed();
    }

    private void EnsureNotClosed()
    {
        if (_state.Status == TicketStatus.Fechado)
        {
            throw new DomainRuleViolationException("Chamado fechado não aceita alterações.");
        }
    }

    // AguardandoSolicitante pausa o relógio; sair dele soma o tempo pausado ao prazo.
    private static TicketSnapshot ApplySla(TicketSnapshot state, TicketStatus from, TicketStatus to, DateTimeOffset now)
    {
        if (to == TicketStatus.AguardandoSolicitante)
        {
            return state with { PausedAt = now };
        }

        if (from == TicketStatus.AguardandoSolicitante && state.PausedAt is { } pausedAt)
        {
            return state with { PausedAt = null, SlaDeadline = state.SlaDeadline?.Add(now - pausedAt) };
        }

        return state;
    }

    private static TicketSnapshot ApplyResolution(TicketSnapshot state, TicketStatus from, TicketStatus to, DateTimeOffset now) =>
        to switch
        {
            TicketStatus.Resolvido => state with { ResolvedAt = now },
            _ when TicketStateMachine.IsReopen(from, to) => state with { ResolvedAt = null },
            _ => state,
        };

    private static void EnsureText(string? value, int maxLength, string field)
    {
        EnsureNotBlank(value, $"{field} obrigatório.");
        if (value!.Trim().Length > maxLength)
        {
            throw new DomainRuleViolationException($"{field} excede {maxLength} caracteres.");
        }
    }

    private static void EnsureNotBlank(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException(message);
        }
    }
}
