using Cards.Domain;
using Cards.Domain.Cards;

namespace Cards.UnitTests.Domain;

public class CardStatusTests
{
    [Theory]
    [InlineData("ACTIVE", CardStatus.Active)]
    [InlineData("BLOCKED", CardStatus.Blocked)]
    [InlineData("CANCELLED", CardStatus.Cancelled)]
    [InlineData("active", CardStatus.Active)]
    [InlineData(" Blocked ", CardStatus.Blocked)]
    public void Parse_AcceptsAllowedValues(string raw, CardStatus expected)
    {
        Assert.Equal(expected, CardStatusExtensions.Parse(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EXPIRED")]
    [InlineData("DELETED")]
    public void Parse_RejectsUnknownValues(string? raw)
    {
        Assert.Throws<DomainValidationException>(() => CardStatusExtensions.Parse(raw));
    }

    [Fact]
    public void ToWireValue_RoundTrips()
    {
        foreach (var status in Enum.GetValues<CardStatus>())
        {
            Assert.Equal(status, CardStatusExtensions.Parse(status.ToWireValue()));
        }
    }
}
