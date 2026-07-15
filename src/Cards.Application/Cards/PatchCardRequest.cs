namespace Cards.Application.Cards;

/// <summary>
/// Merge-style partial update: only the fields present in the payload are
/// applied; absent fields keep their current values. Clearing the nickname
/// requires a PUT.
/// </summary>
public sealed record PatchCardRequest
{
    /// <example>MARIANA ALVES</example>
    public string? CardholderName { get; init; }

    /// <example>Uso diário</example>
    public string? Nickname { get; init; }

    /// <example>VISA</example>
    public string? Brand { get; init; }

    public string? CardNumber { get; init; }

    /// <example>2030-01-31</example>
    public DateOnly? ExpirationDate { get; init; }

    /// <example>15500.00</example>
    public decimal? CreditLimit { get; init; }

    /// <example>BLOCKED</example>
    public string? Status { get; init; }

    public string? Pin { get; init; }
}
