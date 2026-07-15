namespace Cards.Application.Cards;

/// <summary>Returned only by the dedicated PIN endpoint, with no-store cache headers.</summary>
public sealed record PinResponse
{
    /// <example>1234</example>
    public required string Pin { get; init; }
}
