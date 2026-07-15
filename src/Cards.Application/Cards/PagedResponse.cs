namespace Cards.Application.Cards;

public sealed record PagedResponse<T>
{
    /// <example>1</example>
    public required int Page { get; init; }

    /// <example>10</example>
    public required int PageSize { get; init; }

    /// <example>12</example>
    public required int TotalItems { get; init; }

    /// <example>2</example>
    public required int TotalPages { get; init; }

    public required IReadOnlyList<T> Items { get; init; }
}
