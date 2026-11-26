using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Domain.ValueObjects;
using Xunit;

namespace Comprove.HelpDesk.Tests.Unit.Domain;

public sealed class ProtocolNumberTests
{
    [Fact]
    public void Creates_global_protocol_in_canonical_format()
    {
        Assert.Equal("SUP-2026-000001", ProtocolNumber.Create(2026, 1).Value);
    }

    [Fact]
    public void Parse_accepts_canonical_and_rejects_other_formats()
    {
        Assert.Equal("SUP-2026-000042", ProtocolNumber.Parse("SUP-2026-000042").Value);
        Assert.Throws<DomainRuleViolationException>(() => ProtocolNumber.Parse("HD-2041"));
    }

    [Fact]
    public void Rejects_non_positive_sequence()
    {
        Assert.Throws<DomainRuleViolationException>(() => ProtocolNumber.Create(2026, 0));
    }
}
