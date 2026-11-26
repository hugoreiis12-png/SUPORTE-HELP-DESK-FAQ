using Comprove.HelpDesk.Domain.ValueObjects;
using Xunit;

namespace Comprove.HelpDesk.Tests.Unit.Domain;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("+55 (21) 99999-9999", "5521999999999")]
    [InlineData("5521999999999", "5521999999999")]
    [InlineData("(21) 99999-9999", "5521999999999")]
    [InlineData("2133334444", "552133334444")]
    [InlineData("+1 415 555 0100", "14155550100")]
    public void Normalizes_to_international_digits(string raw, string expected) =>
        Assert.Equal(expected, PhoneNumber.Normalize(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("99999-9999")]
    [InlineData("ramal 123")]
    [InlineData("21999999999@c.us")]
    [InlineData("1234567890123456")]
    public void Rejects_what_cannot_be_dialed(string? raw) =>
        Assert.Null(PhoneNumber.Normalize(raw));
}
