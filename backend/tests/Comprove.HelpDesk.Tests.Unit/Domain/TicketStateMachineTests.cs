using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.Rules;
using Xunit;

namespace Comprove.HelpDesk.Tests.Unit.Domain;

public sealed class TicketStateMachineTests
{
    [Theory]
    [InlineData(TicketStatus.Recebido, TicketStatus.EmTriagem)]
    [InlineData(TicketStatus.EmTriagem, TicketStatus.AguardandoAtendimento)]
    [InlineData(TicketStatus.AguardandoAtendimento, TicketStatus.EmAtendimento)]
    [InlineData(TicketStatus.EmAtendimento, TicketStatus.AguardandoSolicitante)]
    [InlineData(TicketStatus.AguardandoSolicitante, TicketStatus.EmAtendimento)]
    [InlineData(TicketStatus.EmAtendimento, TicketStatus.Resolvido)]
    [InlineData(TicketStatus.Resolvido, TicketStatus.Fechado)]
    public void Allows_documented_transitions(TicketStatus from, TicketStatus to)
    {
        Assert.True(TicketStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(TicketStatus.Recebido, TicketStatus.Resolvido)]
    [InlineData(TicketStatus.Fechado, TicketStatus.EmAtendimento)]
    [InlineData(TicketStatus.Recebido, TicketStatus.EmAtendimento)]
    public void Rejects_forbidden_transitions(TicketStatus from, TicketStatus to)
    {
        Assert.False(TicketStateMachine.CanTransition(from, to));
        Assert.Throws<DomainRuleViolationException>(() => TicketStateMachine.EnsureAllowed(from, to, "motivo"));
    }

    [Fact]
    public void Reopen_requires_reason()
    {
        Assert.Throws<DomainRuleViolationException>(() =>
            TicketStateMachine.EnsureAllowed(TicketStatus.Resolvido, TicketStatus.EmAtendimento, " "));
    }

    [Fact]
    public void Reopen_with_reason_is_allowed()
    {
        TicketStateMachine.EnsureAllowed(TicketStatus.Resolvido, TicketStatus.EmAtendimento, "cliente voltou");
        Assert.True(TicketStateMachine.IsReopen(TicketStatus.Resolvido, TicketStatus.EmAtendimento));
    }
}
