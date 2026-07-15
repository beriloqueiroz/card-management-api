using System.Globalization;
using Cards.Application.Abstractions;
using Cards.Domain.Cards;
using Cards.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Cards.Infrastructure.Persistence.Seeding;

/// <summary>
/// Replays the data mass from prova_cartoes_seed_postgresql.sql. The provided
/// script has no PIN column, so every seeded card receives the documented
/// default PIN (encrypted). IDs are fixed to make manual testing reproducible.
/// </summary>
public sealed class DatabaseSeeder(CardsDbContext db, IPinCipher pinCipher)
{
    public const string DefaultSeedPin = "1234";

    public static readonly Guid MarianaId = Guid.Parse("00000000-0000-4000-8000-000000000001");
    public static readonly Guid RafaelId = Guid.Parse("00000000-0000-4000-8000-000000000002");
    public static readonly Guid CamilaId = Guid.Parse("00000000-0000-4000-8000-000000000003");

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        db.Users.AddRange(
            new User(MarianaId, "Mariana Alves", "mariana.alves@cardcorp.test", At("2026-01-10T09:00:00Z")),
            new User(RafaelId, "Rafael Souza", "rafael.souza@cardcorp.test", At("2026-01-11T10:30:00Z")),
            new User(CamilaId, "Camila Rocha", "camila.rocha@cardcorp.test", At("2026-01-12T14:45:00Z")));

        // Mariana Alves - 12 cards to exercise the fixed 10-item pagination.
        db.CreditCards.AddRange(
            Card(1, MarianaId, "MARIANA ALVES", "Principal", "VISA", "5321", "5336", "2028-01-31", 12000.00m, "ACTIVE", "2026-06-30T09:10:00Z"),
            Card(2, MarianaId, "MARIANA ALVES", "Viagens", "MASTERCARD", "5412", "1002", "2028-02-29", 8500.00m, "ACTIVE", "2026-06-29T11:20:00Z"),
            Card(3, MarianaId, "MARIANA ALVES", "Corporativo", "VISA", "4532", "1003", "2028-03-31", 15000.00m, "BLOCKED", "2026-06-28T15:40:00Z"),
            Card(4, MarianaId, "MARIANA ALVES", "Compras Online", "ELO", "6505", "1004", "2028-04-30", 3000.00m, "ACTIVE", "2026-06-27T08:05:00Z"),
            Card(5, MarianaId, "MARIANA ALVES", "Assinaturas", "VISA", "4984", "1005", "2028-05-31", 2500.00m, "ACTIVE", "2026-06-26T20:15:00Z"),
            Card(6, MarianaId, "MARIANA ALVES", "Reserva", "MASTERCARD", "5522", "1006", "2028-06-30", 5000.00m, "ACTIVE", "2026-06-25T13:35:00Z"),
            Card(7, MarianaId, "MARIANA ALVES", "Premium", "AMEX", "3714", "1007", "2028-07-31", 20000.00m, "ACTIVE", "2026-06-24T16:25:00Z"),
            Card(8, MarianaId, "MARIANA ALVES", "Digital", "VISA", "4012", "1008", "2028-08-31", 4000.00m, "ACTIVE", "2026-06-23T12:00:00Z"),
            Card(9, MarianaId, "MARIANA ALVES", "Beneficios", "ELO", "6362", "1009", "2028-09-30", 6000.00m, "CANCELLED", "2026-06-22T10:45:00Z"),
            Card(10, MarianaId, "MARIANA ALVES", "Emergencia", "MASTERCARD", "5555", "1010", "2028-10-31", 7000.00m, "ACTIVE", "2026-06-21T09:30:00Z"),
            Card(11, MarianaId, "MARIANA ALVES", "Internacional", "VISA", "4321", "1011", "2028-11-30", 11000.00m, "ACTIVE", "2026-06-20T17:50:00Z"),
            Card(12, MarianaId, "MARIANA ALVES", "Antigo", "MASTERCARD", "5100", "1012", "2028-12-31", 4500.00m, "BLOCKED", "2026-06-19T18:05:00Z"),
            // Rafael Souza - 4 cards.
            Card(13, RafaelId, "RAFAEL SOUZA", "Principal", "VISA", "4111", "2201", "2029-01-31", 9000.00m, "ACTIVE", "2026-06-18T14:10:00Z"),
            Card(14, RafaelId, "RAFAEL SOUZA", "Empresa", "MASTERCARD", "5454", "2202", "2029-02-28", 13000.00m, "ACTIVE", "2026-06-17T09:35:00Z"),
            Card(15, RafaelId, "RAFAEL SOUZA", "Streaming", "ELO", "6504", "2203", "2029-03-31", 1500.00m, "BLOCKED", "2026-06-16T21:15:00Z"),
            Card(16, RafaelId, "RAFAEL SOUZA", "Backup", "VISA", "4012", "2204", "2029-04-30", 3000.00m, "ACTIVE", "2026-06-15T07:50:00Z"),
            // Camila Rocha - 7 cards.
            Card(17, CamilaId, "CAMILA ROCHA", "Principal", "MASTERCARD", "5556", "3301", "2030-01-31", 10000.00m, "ACTIVE", "2026-06-14T12:15:00Z"),
            Card(18, CamilaId, "CAMILA ROCHA", "Viagens", "VISA", "4024", "3302", "2030-02-28", 9500.00m, "ACTIVE", "2026-06-13T19:30:00Z"),
            Card(19, CamilaId, "CAMILA ROCHA", "Online", "ELO", "6516", "3303", "2030-03-31", 2500.00m, "ACTIVE", "2026-06-12T10:40:00Z"),
            Card(20, CamilaId, "CAMILA ROCHA", "Mercado", "VISA", "4000", "3304", "2030-04-30", 4000.00m, "BLOCKED", "2026-06-11T08:25:00Z"),
            Card(21, CamilaId, "CAMILA ROCHA", "Corporativo", "MASTERCARD", "5312", "3305", "2030-05-31", 16000.00m, "ACTIVE", "2026-06-10T16:00:00Z"),
            Card(22, CamilaId, "CAMILA ROCHA", "Reserva", "VISA", "4929", "3306", "2030-06-30", 5000.00m, "ACTIVE", "2026-06-09T11:55:00Z"),
            Card(23, CamilaId, "CAMILA ROCHA", "Antigo", "AMEX", "3782", "3307", "2030-07-31", 7000.00m, "CANCELLED", "2026-06-08T09:45:00Z"));

        await db.SaveChangesAsync(ct);
    }

    private CreditCard Card(
        int number,
        Guid userId,
        string cardholderName,
        string nickname,
        string brand,
        string firstFour,
        string lastFour,
        string expirationDate,
        decimal creditLimit,
        string status,
        string createdAt) =>
        CreditCard.Restore(
            id: Guid.Parse($"00000000-0000-4000-9000-{number:d12}"),
            userId,
            cardholderName,
            nickname,
            brand,
            firstFour,
            lastFour,
            DateOnly.Parse(expirationDate, CultureInfo.InvariantCulture),
            creditLimit,
            CardStatusExtensions.Parse(status),
            pinCipher.Encrypt(Pin.Create(DefaultSeedPin)),
            externalId: null,
            At(createdAt),
            updatedAt: null,
            deletedAt: null);

    private static DateTimeOffset At(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);
}
