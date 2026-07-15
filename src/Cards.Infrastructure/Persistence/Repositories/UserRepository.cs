using Cards.Application.Abstractions;
using Cards.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Cards.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(CardsDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, ct);
}
