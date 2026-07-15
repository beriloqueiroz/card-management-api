using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cards.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef migrations` can build the model without
/// booting the API. `migrations add` never opens a connection — the fallback
/// below is a placeholder; export CARDS_CONNECTION_STRING to target a real
/// database with `dotnet ef database update`.
/// </summary>
public sealed class CardsDbContextFactory : IDesignTimeDbContextFactory<CardsDbContext>
{
    public CardsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CARDS_CONNECTION_STRING")
            ?? "Host=localhost;Database=cards;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CardsDbContext(options);
    }
}
