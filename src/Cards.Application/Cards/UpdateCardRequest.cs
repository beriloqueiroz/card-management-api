namespace Cards.Application.Cards;

/// <summary>
/// PUT payload: full replacement of the editable fields. Every field except
/// nickname is required; a missing nickname clears the stored one.
/// </summary>
public sealed record UpdateCardRequest
{
    /// <example>MARIANA ALVES</example>
    public string? CardholderName { get; init; }

    /// <example>Principal Atualizado</example>
    public string? Nickname { get; init; }

    /// <example>VISA</example>
    public string? Brand { get; init; }

    /// <example>5321123412345336</example>
    public string? CardNumber { get; init; }

    /// <example>2029-01-31</example>
    public DateOnly? ExpirationDate { get; init; }

    /// <example>14000.00</example>
    public decimal? CreditLimit { get; init; }

    /// <example>ACTIVE</example>
    public string? Status { get; init; }

    /// <example>9876</example>
    public string? Pin { get; init; }
}
