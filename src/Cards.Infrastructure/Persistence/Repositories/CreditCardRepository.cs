using Cards.Application.Abstractions;
using Cards.Domain.Cards;
using Microsoft.EntityFrameworkCore;

namespace Cards.Infrastructure.Persistence.Repositories;

internal sealed class CreditCardRepository(CardsDbContext db) : ICreditCardRepository
{
    public Task<CreditCard?> GetByIdAsync(Guid userId, Guid cardId, CancellationToken ct = default) =>
        db.CreditCards.SingleOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, ct);

    public async Task<CardPage> GetPageAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? expirationDateFrom,
        DateOnly? expirationDateTo,
        CancellationToken ct = default)
    {
        IQueryable<CreditCard> query = db.CreditCards.AsNoTracking().Where(c => c.UserId == userId);

        if (expirationDateFrom is { } from)
        {
            query = query.Where(c => c.ExpirationDate >= from);
        }

        if (expirationDateTo is { } to)
        {
            query = query.Where(c => c.ExpirationDate <= to);
        }

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new CardPage(items, totalItems);
    }

    public async Task AddAsync(CreditCard card, CancellationToken ct = default) =>
        await db.CreditCards.AddAsync(card, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
