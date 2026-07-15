namespace Cards.Domain.Cards;

/// <summary>
/// Validates a full PAN and keeps only the first/last four digits.
/// The full number is never stored, reducing PCI scope. Luhn is deliberately
/// not enforced: the sample PANs in the challenge spec fail the checksum.
/// </summary>
public sealed record CardNumber
{
    public string FirstFour { get; }
    public string LastFour { get; }

    private CardNumber(string firstFour, string lastFour)
    {
        FirstFour = firstFour;
        LastFour = lastFour;
    }

    public static CardNumber Create(string? raw)
    {
        var digits = raw?.Trim();
        if (string.IsNullOrEmpty(digits) || !digits.All(char.IsAsciiDigit) || digits.Length is < 13 or > 19)
        {
            throw new DomainValidationException("cardNumber must contain 13 to 19 numeric digits.");
        }

        return new CardNumber(digits[..4], digits[^4..]);
    }
}
