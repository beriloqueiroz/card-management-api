namespace Cards.Domain.Cards;

public enum CardStatus
{
    Active,
    Blocked,
    Cancelled,
}

public static class CardStatusExtensions
{
    public static string ToWireValue(this CardStatus status) => status switch
    {
        CardStatus.Active => "ACTIVE",
        CardStatus.Blocked => "BLOCKED",
        CardStatus.Cancelled => "CANCELLED",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static CardStatus Parse(string? value)
    {
        if (!TryParse(value, out var status))
        {
            throw new DomainValidationException("status must be one of: ACTIVE, BLOCKED, CANCELLED.");
        }

        return status;
    }

    public static bool TryParse(string? value, out CardStatus status)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case "ACTIVE":
                status = CardStatus.Active;
                return true;
            case "BLOCKED":
                status = CardStatus.Blocked;
                return true;
            case "CANCELLED":
                status = CardStatus.Cancelled;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
