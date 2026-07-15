namespace Cards.Application.Cards;

/// <summary>
/// cardNumber and pin are write-only sensitive fields: accepted here, never
/// echoed back in any response, log or error message.
/// </summary>
public sealed record CreateCardRequest
{
    /// <example>MARIANA ALVES</example>
    public string? CardholderName { get; init; }

    /// <example>Cartão Eventos</example>
    public string? Nickname { get; init; }

    /// <example>VISA</example>
    public string? Brand { get; init; }

    /// <example>5321123412345336</example>
    public string? CardNumber { get; init; }

    /// <example>2029-12-31</example>
    public DateOnly? ExpirationDate { get; init; }

    /// <example>6500.00</example>
    public decimal? CreditLimit { get; init; }

    /// <summary>Optional on creation; defaults to ACTIVE.</summary>
    /// <example>ACTIVE</example>
    public string? Status { get; init; }

    /// <example>1234</example>
    public string? Pin { get; init; }
}
