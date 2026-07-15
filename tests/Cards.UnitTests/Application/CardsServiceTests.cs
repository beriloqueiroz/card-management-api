using Cards.Application;
using Cards.Application.Cards;
using Cards.Domain;
using Cards.Domain.Cards;
using Cards.UnitTests.Fakes;

namespace Cards.UnitTests.Application;

public class CardsServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-15T12:00:00Z");
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private readonly FakeCreditCardRepository _repository = new();
    private readonly FakePinCipher _pinCipher = new();
    private readonly RecordingAuditLogger _audit = new();
    private readonly CardsService _service;

    public CardsServiceTests()
    {
        _service = new CardsService(_repository, _pinCipher, new FixedTimeProvider(Now), _audit);
    }

    private CreditCard AddCard(Guid userId, string nickname, DateOnly expiration, DateTimeOffset createdAt)
    {
        var card = CreditCard.Restore(
            Guid.NewGuid(), userId, "HOLDER NAME", nickname, "VISA", "5321", "5336",
            expiration, 1000m, CardStatus.Active, _pinCipher.Encrypt(Pin.Create("1234")),
            null, createdAt, null, null);
        _repository.Cards.Add(card);
        return card;
    }

    private static CreateCardRequest ValidCreate() => new()
    {
        CardholderName = "MARIANA ALVES",
        Nickname = "Cartão Eventos",
        Brand = "VISA",
        CardNumber = "5321123412345336",
        ExpirationDate = new DateOnly(2029, 12, 31),
        CreditLimit = 6500.00m,
        Pin = "1234",
    };

    // --- listing -----------------------------------------------------------

    [Fact]
    public async Task ListAsync_ReturnsFixedPagesOfTenNewestFirst()
    {
        for (var i = 1; i <= 12; i++)
        {
            AddCard(UserId, $"card-{i}", new DateOnly(2028, 1, 31), Now.AddDays(-i));
        }

        var page1 = await _service.ListAsync(UserId, 1, null, null);
        var page2 = await _service.ListAsync(UserId, 2, null, null);

        Assert.Equal(10, page1.PageSize);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(12, page1.TotalItems);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal("card-1", page1.Items[0].Nickname); // newest first
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal("card-12", page2.Items[^1].Nickname);
    }

    [Fact]
    public async Task ListAsync_FiltersByExpirationPeriod()
    {
        AddCard(UserId, "in-range", new DateOnly(2028, 6, 30), Now);
        AddCard(UserId, "out-of-range", new DateOnly(2030, 6, 30), Now);

        var result = await _service.ListAsync(
            UserId, 1, new DateOnly(2028, 1, 1), new DateOnly(2028, 12, 31));

        Assert.Single(result.Items);
        Assert.Equal("in-range", result.Items[0].Nickname);
    }

    [Fact]
    public async Task ListAsync_RejectsInvalidPageAndInvertedPeriod()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.ListAsync(UserId, 0, null, null));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.ListAsync(UserId, 1, new DateOnly(2029, 1, 1), new DateOnly(2028, 1, 1)));
    }

    [Fact]
    public async Task ListAsync_NeverReturnsOtherUsersCards()
    {
        AddCard(OtherUserId, "not-mine", new DateOnly(2028, 1, 31), Now);

        var result = await _service.ListAsync(UserId, 1, null, null);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
    }

    // --- create ------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_DefaultsToActiveAndMasksNumber()
    {
        var response = await _service.CreateAsync(UserId, ValidCreate());

        Assert.Equal("ACTIVE", response.Status);
        Assert.Equal("5321 **** **** 5336", response.MaskedNumber);
        Assert.Equal(1, _repository.SaveChangesCalls);
        var stored = Assert.Single(_repository.Cards);
        Assert.Equal("1234", _pinCipher.Decrypt(stored.PinEncrypted).Value);
    }

    [Theory]
    [InlineData("EXPIRED")]
    [InlineData("foo")]
    public async Task CreateAsync_RejectsInvalidStatus(string status)
    {
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync(UserId, ValidCreate() with { Status = status }));
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingRequiredFields()
    {
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync(UserId, ValidCreate() with { ExpirationDate = null }));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync(UserId, ValidCreate() with { CreditLimit = null }));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync(UserId, ValidCreate() with { Pin = null }));
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync(UserId, ValidCreate() with { CardNumber = null }));
    }

    // --- update ------------------------------------------------------------

    [Fact]
    public async Task ReplaceAsync_ReplacesEditableFields()
    {
        var card = AddCard(UserId, "old", new DateOnly(2028, 1, 31), Now);

        var response = await _service.ReplaceAsync(UserId, card.Id, new UpdateCardRequest
        {
            CardholderName = "NEW NAME",
            Nickname = null,
            Brand = "MASTERCARD",
            CardNumber = "5412123412341002",
            ExpirationDate = new DateOnly(2030, 1, 31),
            CreditLimit = 14000.00m,
            Status = "BLOCKED",
            Pin = "9876",
        });

        Assert.Equal("NEW NAME", response.CardholderName);
        Assert.Null(response.Nickname); // PUT clears absent nickname
        Assert.Equal("5412 **** **** 1002", response.MaskedNumber);
        Assert.Equal("BLOCKED", response.Status);
        Assert.Equal(Now, response.UpdatedAt);
        Assert.Equal("9876", _pinCipher.Decrypt(card.PinEncrypted).Value);
    }

    [Fact]
    public async Task PatchAsync_AppliesOnlyProvidedFields()
    {
        var card = AddCard(UserId, "Principal", new DateOnly(2028, 1, 31), Now);

        var response = await _service.PatchAsync(UserId, card.Id, new PatchCardRequest
        {
            Nickname = "Uso diário",
            CreditLimit = 15500.00m,
        });

        Assert.Equal("Uso diário", response.Nickname);
        Assert.Equal(15500.00m, response.CreditLimit);
        Assert.Equal("HOLDER NAME", response.CardholderName); // untouched
        Assert.Equal(Now, response.UpdatedAt);
    }

    [Fact]
    public async Task PatchAsync_RejectsEmptyPayload()
    {
        var card = AddCard(UserId, "Principal", new DateOnly(2028, 1, 31), Now);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.PatchAsync(UserId, card.Id, new PatchCardRequest()));
    }

    // --- ownership / not found ---------------------------------------------

    [Fact]
    public async Task Operations_OnAnotherUsersCard_LookLikeNotFound()
    {
        var foreign = AddCard(OtherUserId, "not-mine", new DateOnly(2028, 1, 31), Now);

        await Assert.ThrowsAsync<CardNotFoundException>(() => _service.GetAsync(UserId, foreign.Id));
        await Assert.ThrowsAsync<CardNotFoundException>(() => _service.DeleteAsync(UserId, foreign.Id));
        await Assert.ThrowsAsync<CardNotFoundException>(() => _service.RevealPinAsync(UserId, foreign.Id));
        await Assert.ThrowsAsync<CardNotFoundException>(() =>
            _service.PatchAsync(UserId, foreign.Id, new PatchCardRequest { Nickname = "x" }));
    }

    // --- delete --------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_SoftDeletesAndHidesFromQueries()
    {
        var card = AddCard(UserId, "Principal", new DateOnly(2028, 1, 31), Now);

        await _service.DeleteAsync(UserId, card.Id);

        Assert.True(card.IsDeleted);
        Assert.Single(_repository.Cards); // row is preserved
        await Assert.ThrowsAsync<CardNotFoundException>(() => _service.GetAsync(UserId, card.Id));
        var list = await _service.ListAsync(UserId, 1, null, null);
        Assert.Empty(list.Items);
    }

    // --- PIN -----------------------------------------------------------------

    [Fact]
    public async Task RevealPinAsync_ReturnsOriginalPinAndAudits()
    {
        var card = AddCard(UserId, "Principal", new DateOnly(2028, 1, 31), Now);

        var response = await _service.RevealPinAsync(UserId, card.Id);

        Assert.Equal("1234", response.Pin);
        Assert.Equal([(UserId, card.Id)], _audit.PinReveals);
    }
}
