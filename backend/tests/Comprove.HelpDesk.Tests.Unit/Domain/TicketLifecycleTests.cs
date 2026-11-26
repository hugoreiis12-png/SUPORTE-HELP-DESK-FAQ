using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.Rules;
using Comprove.HelpDesk.Domain.ValueObjects;
using Xunit;

namespace Comprove.HelpDesk.Tests.Unit.Domain;

public sealed class TicketLifecycleTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private static Ticket OpenCritical() => Ticket.Open(
        "t-1",
        TenantId.From("tenant-a"),
        ProtocolNumber.Create(2026, 1),
        "user-1",
        "cat-1",
        "Sistema fora",
        "Emissão parada",
        ImpactLevel.Alto,
        ScopeLevel.Alto,
        FrequencyLevel.Alta,
        UrgencyLevel.Alta,
        OpenedAt,
        idempotencyKey: null);

    [Fact]
    public void Classify_sets_severity_and_deadline_from_opening_time()
    {
        var ticket = OpenCritical();
        ticket.Classify(SlaPolicy.Default, OpenedAt.AddMinutes(5));

        var state = ticket.Snapshot();
        Assert.Equal(Severity.Crítica, state.Severity);
        Assert.Equal(TicketStatus.EmTriagem, state.Status);
        Assert.Equal(OpenedAt.AddHours(2), state.SlaDeadline);
    }

    [Fact]
    public void Waiting_for_requester_pauses_and_resume_extends_deadline()
    {
        var ticket = OpenCritical();
        ticket.Classify(SlaPolicy.Default, OpenedAt);
        ticket.Dispatch("queue-1", "agent-1", OpenedAt);
        ticket.Transition(TicketStatus.AguardandoSolicitante, OpenedAt.AddMinutes(30), null);
        ticket.Transition(TicketStatus.EmAtendimento, OpenedAt.AddMinutes(90), null);

        var state = ticket.Snapshot();
        Assert.Null(state.PausedAt);
        Assert.Equal(OpenedAt.AddHours(2).AddMinutes(60), state.SlaDeadline);
    }

    [Fact]
    public void Resolve_sets_resolved_at_and_reopen_clears_it()
    {
        var ticket = OpenCritical();
        ticket.Classify(SlaPolicy.Default, OpenedAt);
        ticket.Dispatch("queue-1", "agent-1", OpenedAt);
        ticket.Transition(TicketStatus.Resolvido, OpenedAt.AddHours(1), null);
        Assert.NotNull(ticket.Snapshot().ResolvedAt);

        ticket.Transition(TicketStatus.EmAtendimento, OpenedAt.AddHours(2), "cliente voltou");
        Assert.Null(ticket.Snapshot().ResolvedAt);
    }

    [Fact]
    public void Closed_ticket_rejects_messages()
    {
        var ticket = OpenCritical();
        ticket.Classify(SlaPolicy.Default, OpenedAt);
        ticket.Dispatch("queue-1", "agent-1", OpenedAt);
        ticket.Transition(TicketStatus.Resolvido, OpenedAt, null);
        ticket.Transition(TicketStatus.Fechado, OpenedAt, null);

        Assert.Throws<DomainRuleViolationException>(() => ticket.EnsureCanReceiveMessages());
    }

    [Fact]
    public void Open_rejects_blank_requester()
    {
        Assert.Throws<DomainRuleViolationException>(() => Ticket.Open(
            "t-2", TenantId.From("tenant-a"), ProtocolNumber.Create(2026, 2), " ", "cat-1", "Título", "Descrição",
            ImpactLevel.Baixo, ScopeLevel.Baixo, FrequencyLevel.Baixa, UrgencyLevel.Baixa, OpenedAt, null));
    }
}
