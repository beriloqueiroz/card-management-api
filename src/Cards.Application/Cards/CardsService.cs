using Cards.Application.Abstractions;
using Cards.Domain;
using Cards.Domain.Cards;

namespace Cards.Application.Cards;

public sealed class CardsService(
    ICreditCardRepository cards,
    IPinCipher pinCipher,
    TimeProvider clock,
    ICardAuditLogger audit) : ICardsService
{
    /// <summary>Fixed by the challenge: listing always navigates in blocks of 10.</summary>
    public const int PageSize = 10;

    public async Task<PagedResponse<CardResponse>> ListAsync(
        Guid userId,
        int page,
        DateOnly? expirationDateFrom,
        DateOnly? expirationDateTo,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            throw new DomainValidationException("page must be greater than or equal to 1.");
        }

        if (expirationDateFrom > expirationDateTo)
        {
            throw new DomainValidationException(
                "expirationDateFrom must be earlier than or equal to expirationDateTo.");
        }

        var result = await cards.GetPageAsync(userId, page, PageSize, expirationDateFrom, expirationDateTo, ct);

        return new PagedResponse<CardResponse>
        {
            Page = page,
            PageSize = PageSize,
            TotalItems = result.TotalItems,
            TotalPages = (int)Math.Ceiling(result.TotalItems / (double)PageSize),
            Items = result.Items.Select(CardResponse.From).ToList(),
        };
    }

    public async Task<CardResponse> GetAsync(Guid userId, Guid cardId, CancellationToken ct = default) =>
        CardResponse.From(await GetOwnedCardAsync(userId, cardId, ct));

    public async Task<CardResponse> CreateAsync(
        Guid userId,
        CreateCardRequest request,
        CancellationToken ct = default)
    {
        var status = request.Status is null ? CardStatus.Active : CardStatusExtensions.Parse(request.Status);
        var card = CreditCard.Create(
            userId,
            request.CardholderName,
            request.Nickname,
            request.Brand,
            CardNumber.Create(request.CardNumber),
            RequiredExpirationDate(request.ExpirationDate),
            RequiredCreditLimit(request.CreditLimit),
            status,
            pinCipher.Encrypt(Pin.Create(request.Pin)),
            clock.GetUtcNow());

        await cards.AddAsync(card, ct);
        await cards.SaveChangesAsync(ct);
        return CardResponse.From(card);
    }

    public async Task<CardResponse> ReplaceAsync(
        Guid userId,
        Guid cardId,
        UpdateCardRequest request,
        CancellationToken ct = default)
    {
        var card = await GetOwnedCardAsync(userId, cardId, ct);

        card.ReplaceEditableData(
            request.CardholderName,
            request.Nickname,
            request.Brand,
            CardNumber.Create(request.CardNumber),
            RequiredExpirationDate(request.ExpirationDate),
            RequiredCreditLimit(request.CreditLimit),
            CardStatusExtensions.Parse(request.Status),
            pinCipher.Encrypt(Pin.Create(request.Pin)),
            clock.GetUtcNow());

        await cards.SaveChangesAsync(ct);
        return CardResponse.From(card);
    }

    public async Task<CardResponse> PatchAsync(
        Guid userId,
        Guid cardId,
        PatchCardRequest request,
        CancellationToken ct = default)
    {
        var card = await GetOwnedCardAsync(userId, cardId, ct);

        // The service only translates wire values into domain types; the
        // merge semantics live in the entity.
        var changed = card.ApplyPartialChanges(
            request.CardholderName,
            request.Nickname,
            request.Brand,
            request.CardNumber is null ? null : CardNumber.Create(request.CardNumber),
            request.ExpirationDate,
            request.CreditLimit,
            request.Status is null ? null : CardStatusExtensions.Parse(request.Status),
            request.Pin is null ? null : pinCipher.Encrypt(Pin.Create(request.Pin)),
            clock.GetUtcNow());

        if (!changed)
        {
            throw new DomainValidationException("at least one editable field must be provided.");
        }

        await cards.SaveChangesAsync(ct);
        return CardResponse.From(card);
    }

    public async Task DeleteAsync(Guid userId, Guid cardId, CancellationToken ct = default)
    {
        var card = await GetOwnedCardAsync(userId, cardId, ct);
        card.SoftDelete(clock.GetUtcNow());
        await cards.SaveChangesAsync(ct);
    }

    public async Task<PinResponse> RevealPinAsync(Guid userId, Guid cardId, CancellationToken ct = default)
    {
        var card = await GetOwnedCardAsync(userId, cardId, ct);
        var pin = pinCipher.Decrypt(card.PinEncrypted);

        audit.PinRevealed(userId, card.Id);

        return new PinResponse { Pin = pin.Value };
    }

    private async Task<CreditCard> GetOwnedCardAsync(Guid userId, Guid cardId, CancellationToken ct) =>
        await cards.GetByIdAsync(userId, cardId, ct) ?? throw new CardNotFoundException();

    private static DateOnly RequiredExpirationDate(DateOnly? value) =>
        value ?? throw new DomainValidationException("expirationDate is required (format: yyyy-MM-dd).");

    private static decimal RequiredCreditLimit(decimal? value) =>
        value ?? throw new DomainValidationException("creditLimit is required.");
}
