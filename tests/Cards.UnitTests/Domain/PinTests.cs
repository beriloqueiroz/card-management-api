using Cards.Domain;
using Cards.Domain.Cards;

namespace Cards.UnitTests.Domain;

public class PinTests
{
    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    public void Create_AcceptsFourToSixDigits(string raw)
    {
        Assert.Equal(raw, Pin.Create(raw).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567")]
    [InlineData("12a4")]
    public void Create_RejectsInvalidInput(string? raw)
    {
        Assert.Throws<DomainValidationException>(() => Pin.Create(raw));
    }
}
