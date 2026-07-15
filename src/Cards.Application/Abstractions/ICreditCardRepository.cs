using Cards.Domain.Cards;

namespace Cards.Application.Abstractions;

public sealed record CardPage(IReadOnlyList<CreditCard> Items, int TotalItems);

public interface ICreditCardRepository
{
    /// <summary>Scoped by owner: a card from another user is indistinguishable from a missing one.</summary>
    Task<CreditCard?> GetByIdAsync(Guid userId, Guid cardId, CancellationToken ct = default);

    /// <summary>Filtering, ordering and paging run in the database, before materialization.</summary>
    Task<CardPage> GetPageAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? expirationDateFrom,
        DateOnly? expirationDateTo,
        CancellationToken ct = default);

    Task AddAsync(CreditCard card, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
