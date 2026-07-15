using Cards.Application.Abstractions;
using Cards.Domain.Cards;
using Cards.Infrastructure.Persistence;
using Cards.Infrastructure.Persistence.Seeding;
using Cards.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;

namespace Cards.IntegrationTests.Repositories;

/// <summary>
/// Exercises the real repository against PostgreSQL (shared container, own
/// database), seeded with the exact data mass from the challenge's SQL script.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CreditCardRepositoryTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private CardsDbContext _db = null!;
    private ICreditCardRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync("repository_tests");

        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        _db = new CardsDbContext(options);
        await _db.Database.MigrateAsync();
        await new DatabaseSeeder(_db, new FakePinCipher()).SeedAsync();

        _repository = new Cards.Infrastructure.Persistence.Repositories.CreditCardRepository(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task GetPageAsync_PaginatesMarianasTwelveCardsInBlocksOfTen()
    {
        var page1 = await _repository.GetPageAsync(DatabaseSeeder.MarianaId, 1, 10, null, null);
        var page2 = await _repository.GetPageAsync(DatabaseSeeder.MarianaId, 2, 10, null, null);

        Assert.Equal(12, page1.TotalItems);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);

        // Newest first, across the whole set.
        var all = page1.Items.Concat(page2.Items).ToList();
        Assert.Equal(all.OrderByDescending(c => c.CreatedAt).Select(c => c.Id), all.Select(c => c.Id));
        Assert.Equal("Principal", all[0].Nickname);
        Assert.Equal("Antigo", all[^1].Nickname);
    }

    [Fact]
    public async Task GetPageAsync_AppliesExpirationPeriodFilterInDatabase()
    {
        // Mariana's expirations run monthly from 2028-01-31 to 2028-12-31.
        var result = await _repository.GetPageAsync(
            DatabaseSeeder.MarianaId, 1, 10,
            new DateOnly(2028, 1, 1), new DateOnly(2028, 6, 30));

        Assert.Equal(6, result.TotalItems);
        Assert.All(result.Items, c =>
            Assert.InRange(c.ExpirationDate, new DateOnly(2028, 1, 1), new DateOnly(2028, 6, 30)));
    }

    [Fact]
    public async Task GetByIdAsync_IsScopedToTheOwner()
    {
        var marianaCard = (await _repository.GetPageAsync(DatabaseSeeder.MarianaId, 1, 10, null, null)).Items[0];

        var asOwner = await _repository.GetByIdAsync(DatabaseSeeder.MarianaId, marianaCard.Id);
        var asOtherUser = await _repository.GetByIdAsync(DatabaseSeeder.RafaelId, marianaCard.Id);

        Assert.NotNull(asOwner);
        Assert.Null(asOtherUser);
    }

    [Fact]
    public async Task SoftDeletedCards_DisappearFromAllRegularQueries()
    {
        var card = await _repository.GetByIdAsync(
            DatabaseSeeder.RafaelId, Guid.Parse("00000000-0000-4000-9000-000000000013"));
        Assert.NotNull(card);

        card!.SoftDelete(DateTimeOffset.UtcNow);
        await _repository.SaveChangesAsync();

        Assert.Null(await _repository.GetByIdAsync(DatabaseSeeder.RafaelId, card.Id));
        var page = await _repository.GetPageAsync(DatabaseSeeder.RafaelId, 1, 10, null, null);
        Assert.Equal(3, page.TotalItems);
        Assert.DoesNotContain(page.Items, c => c.Id == card.Id);

        // The row survives for traceability (query filter bypassed on purpose).
        var raw = await _db.CreditCards.IgnoreQueryFilters().SingleAsync(c => c.Id == card.Id);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task AddAsync_PersistsRoundTrip()
    {
        var card = CreditCard.Create(
            DatabaseSeeder.CamilaId,
            "CAMILA ROCHA",
            "Novo",
            "VISA",
            CardNumber.Create("4111111111111111"),
            new DateOnly(2031, 5, 31),
            2000m,
            CardStatus.Active,
            new FakePinCipher().Encrypt(Pin.Create("4321")),
            DateTimeOffset.UtcNow);

        await _repository.AddAsync(card);
        await _repository.SaveChangesAsync();

        var reloaded = await _repository.GetByIdAsync(DatabaseSeeder.CamilaId, card.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("4111 **** **** 1111", reloaded!.MaskedNumber);
    }
}
