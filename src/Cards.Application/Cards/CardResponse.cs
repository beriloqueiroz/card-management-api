using Cards.Domain.Cards;

namespace Cards.Application.Cards;

/// <summary>
/// Card representation for regular responses: the number only appears masked
/// and the PIN never appears at all (dedicated endpoint only).
/// </summary>
public sealed record CardResponse
{
    /// <example>00000000-0000-4000-9000-000000000001</example>
    public required Guid Id { get; init; }

    /// <example>MARIANA ALVES</example>
    public required string CardholderName { get; init; }

    /// <example>Principal</example>
    public string? Nickname { get; init; }

    /// <example>VISA</example>
    public required string Brand { get; init; }

    /// <example>5321 **** **** 5336</example>
    public required string MaskedNumber { get; init; }

    /// <example>2028-01-31</example>
    public required DateOnly ExpirationDate { get; init; }

    /// <example>12000.00</example>
    public required decimal CreditLimit { get; init; }

    /// <example>ACTIVE</example>
    public required string Status { get; init; }

    /// <example>2026-06-30T09:10:00Z</example>
    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public static CardResponse From(CreditCard card) => new()
    {
        Id = card.Id,
        CardholderName = card.CardholderName,
        Nickname = card.Nickname,
        Brand = card.Brand,
        MaskedNumber = card.MaskedNumber,
        ExpirationDate = card.ExpirationDate,
        CreditLimit = card.CreditLimit,
        Status = card.Status.ToWireValue(),
        CreatedAt = card.CreatedAt,
        UpdatedAt = card.UpdatedAt,
    };
}
