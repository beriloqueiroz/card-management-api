using Cards.Domain;
using Cards.Domain.Cards;

namespace Cards.UnitTests.Domain;

public class CardNumberTests
{
    [Fact]
    public void Create_KeepsOnlyFirstAndLastFourDigits()
    {
        var number = CardNumber.Create("5321123412345336");

        Assert.Equal("5321", number.FirstFour);
        Assert.Equal("5336", number.LastFour);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("53211234")] // too short
    [InlineData("53211234123453361234")] // too long (20)
    [InlineData("5321-1234-1234-5336")] // non-digits
    [InlineData("5321 1234 1234 5336")]
    public void Create_RejectsInvalidInput(string? raw)
    {
        Assert.Throws<DomainValidationException>(() => CardNumber.Create(raw));
    }

    [Fact]
    public void Create_AcceptsBoundaryLengths()
    {
        Assert.Equal("1234", CardNumber.Create("1234567890123").FirstFour); // 13 digits
        Assert.Equal("4567", CardNumber.Create("1234567890123454567").LastFour); // 19 digits
    }
}
