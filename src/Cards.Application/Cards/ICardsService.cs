namespace Cards.Application.Cards;

public interface ICardsService
{
    Task<PagedResponse<CardResponse>> ListAsync(
        Guid userId,
        int page,
        DateOnly? expirationDateFrom,
        DateOnly? expirationDateTo,
        CancellationToken ct = default);

    Task<CardResponse> GetAsync(Guid userId, Guid cardId, CancellationToken ct = default);

    Task<CardResponse> CreateAsync(Guid userId, CreateCardRequest request, CancellationToken ct = default);

    Task<CardResponse> ReplaceAsync(Guid userId, Guid cardId, UpdateCardRequest request, CancellationToken ct = default);

    Task<CardResponse> PatchAsync(Guid userId, Guid cardId, PatchCardRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid userId, Guid cardId, CancellationToken ct = default);

    Task<PinResponse> RevealPinAsync(Guid userId, Guid cardId, CancellationToken ct = default);
}
