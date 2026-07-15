using Cards.Application.Abstractions;
using Cards.Domain.Cards;

namespace Cards.UnitTests.Fakes;

/// <summary>In-memory repository mirroring the real one's soft-delete and ordering semantics.</summary>
public sealed class FakeCreditCardRepository : ICreditCardRepository
{
    public List<CreditCard> Cards { get; } = [];
    public int SaveChangesCalls { get; private set; }

    public Task<CreditCard?> GetByIdAsync(Guid userId, Guid cardId, CancellationToken ct = default) =>
        Task.FromResult(Cards.FirstOrDefault(c => c.Id == cardId && c.UserId == userId && !c.IsDeleted));

    public Task<CardPage> GetPageAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? expirationDateFrom,
        DateOnly? expirationDateTo,
        CancellationToken ct = default)
    {
        var query = Cards.Where(c => c.UserId == userId && !c.IsDeleted);
        if (expirationDateFrom is { } from)
        {
            query = query.Where(c => c.ExpirationDate >= from);
        }

        if (expirationDateTo is { } to)
        {
            query = query.Where(c => c.ExpirationDate <= to);
        }

        var items = query.OrderByDescending(c => c.CreatedAt).ToList();
        return Task.FromResult(new CardPage(
            items.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            items.Count));
    }

    public Task AddAsync(CreditCard card, CancellationToken ct = default)
    {
        Cards.Add(card);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}
