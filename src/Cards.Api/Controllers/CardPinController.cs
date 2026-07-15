using Cards.Api.Auth;
using Cards.Application.Cards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cards.Api.Controllers;

/// <summary>
/// Dedicated flow for the card PIN, isolated from regular card queries.
/// Every access is audit-logged and the response is marked non-cacheable.
/// </summary>
[ApiController]
[Authorize]
[Route("api/cards/{id:guid}/pin")]
[Produces("application/json")]
public sealed class CardPinController(ICardsService cardsService, ICurrentUserProvider currentUser) : ControllerBase
{
    /// <summary>Reveals the original PIN of a card owned by the authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType<PinResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<PinResponse> Reveal(Guid id, CancellationToken ct = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync(ct);
        var response = await cardsService.RevealPinAsync(userId, id, ct);

        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return response;
    }
}
