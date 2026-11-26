using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.Rules;
using Comprove.HelpDesk.Domain.ValueObjects;
using Xunit;

namespace Comprove.HelpDesk.Tests.Unit.Domain;

public sealed class FaqTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly TenantId Tenant = TenantId.From("tenant-a");
    private static readonly FaqRequester Requester = new("user-1", "Ana", "ana@comprove.com", "Filial 2");

    private static Ticket OpenTicket(ImpactLevel impact, ScopeLevel scope, FrequencyLevel frequency) => Ticket.Open(
        "t-1", Tenant, ProtocolNumber.Create(2026, 7), "user-1", "Pedidos do Campo",
        "Pedido não sincroniza", "O pedido some ao sincronizar", impact, scope, frequency, UrgencyLevel.Media, Now, "key-1");

    private static FaqDraft Draft(string field = "Pedidos do Campo", string bugType = "Falha de Sincronização") =>
        new(field, "Tela de pedidos", bugType, "Pedido duplicado");

    private static Faq OpenFaq(Ticket ticket, FaqDraft draft) => Faq.Open("f-1", Tenant, "FAQ-01", ticket, draft, Requester, Now);

    [Fact]
    public void Open_starts_pending_with_one_ask_and_links_the_ticket()
    {
        var faq = OpenFaq(OpenTicket(ImpactLevel.Medio, ScopeLevel.Medio, FrequencyLevel.Media), Draft());

        Assert.Equal(FaqResolutionStatus.Pendente, faq.ResolutionStatus);
        Assert.Equal(1, faq.AskCount);
        Assert.Equal("t-1", faq.TicketId);
        Assert.Equal("SUP-2026-000007", faq.TicketCode);
        Assert.Equal(Faq.PendingResolutionText, faq.ResolutionText);
    }

    [Theory]
    [InlineData(ImpactLevel.Alto, ScopeLevel.Alto, FrequencyLevel.Alta, Severity.Crítica, 90)]
    [InlineData(ImpactLevel.Medio, ScopeLevel.Medio, FrequencyLevel.Media, Severity.Média, 60)]
    [InlineData(ImpactLevel.Medio, ScopeLevel.Baixo, FrequencyLevel.Media, Severity.Média, 50)]
    [InlineData(ImpactLevel.Baixo, ScopeLevel.Baixo, FrequencyLevel.Baixa, Severity.Baixa, 30)]
    public void Open_classifies_severity_with_the_deterministic_rule(
        ImpactLevel impact, ScopeLevel scope, FrequencyLevel frequency, Severity expected, int score)
    {
        var faq = OpenFaq(OpenTicket(impact, scope, frequency), Draft());

        Assert.Equal(expected, faq.Severity);
        Assert.Equal(score, faq.SeverityScore);
    }

    [Theory]
    [InlineData("Campo inexistente", "Falha de Sincronização")]
    [InlineData("Pedidos do Campo", "Tipo inexistente")]
    public void Open_rejects_values_outside_the_taxonomy(string field, string bugType)
    {
        var ticket = OpenTicket(ImpactLevel.Medio, ScopeLevel.Medio, FrequencyLevel.Media);

        Assert.Throws<DomainRuleViolationException>(() => OpenFaq(ticket, Draft(field, bugType)));
    }

    [Fact]
    public void WithResolution_requires_text_and_trims_it()
    {
        var faq = OpenFaq(OpenTicket(ImpactLevel.Medio, ScopeLevel.Medio, FrequencyLevel.Media), Draft());

        var published = faq.WithResolution(FaqResolutionStatus.SolucaoDeContorno, "  Reiniciar o app  ", Now.AddHours(1));

        Assert.Equal("Reiniciar o app", published.ResolutionText);
        Assert.Equal(FaqResolutionStatus.SolucaoDeContorno, published.ResolutionStatus);
        Assert.Throws<DomainRuleViolationException>(() => faq.WithResolution(FaqResolutionStatus.Resolvido, "   ", Now));
    }

    [Theory]
    [InlineData(TicketStatus.Recebido, FaqResolutionStatus.Pendente)]
    [InlineData(TicketStatus.EmTriagem, FaqResolutionStatus.Pendente)]
    [InlineData(TicketStatus.AguardandoAtendimento, FaqResolutionStatus.Pendente)]
    [InlineData(TicketStatus.EmAtendimento, FaqResolutionStatus.EmAnalise)]
    [InlineData(TicketStatus.AguardandoSolicitante, FaqResolutionStatus.EmAnalise)]
    [InlineData(TicketStatus.Resolvido, FaqResolutionStatus.Resolvido)]
    [InlineData(TicketStatus.Fechado, FaqResolutionStatus.Resolvido)]
    public void Ticket_status_maps_to_the_faq_status_shown_on_screen(TicketStatus ticket, FaqResolutionStatus expected)
    {
        Assert.Equal(expected, FaqStatusRules.FromTicket(ticket));
    }
}
