using Cards.Domain.Cards;
using Cards.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Cards.Infrastructure.Persistence;

public sealed class CardsDbContext(DbContextOptions<CardsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CardsDbContext).Assembly);
}
