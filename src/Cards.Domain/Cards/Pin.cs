namespace Cards.Domain.Cards;

/// <summary>
/// Card usage PIN. Highly sensitive: never logged, never serialized in
/// responses outside the dedicated PIN endpoint.
/// </summary>
public sealed record Pin
{
    public string Value { get; }

    private Pin(string value) => Value = value;

    public static Pin Create(string? raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.Length is < 4 or > 6 || !raw.All(char.IsAsciiDigit))
        {
            throw new DomainValidationException("pin must be 4 to 6 numeric digits.");
        }

        return new Pin(raw);
    }
}
