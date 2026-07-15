using Cards.Api.Auth;
using Cards.Application.Cards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cards.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cards")]
[Produces("application/json")]
public sealed class CardsController(ICardsService cardsService, ICurrentUserProvider currentUser) : ControllerBase
{
    /// <summary>Lists the authenticated user's cards, newest first, in fixed pages of 10.</summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="expirationDateFrom">Inclusive lower bound for the card expiration date (yyyy-MM-dd).</param>
    /// <param name="expirationDateTo">Inclusive upper bound for the card expiration date (yyyy-MM-dd).</param>
    [HttpGet]
    [ProducesResponseType<PagedResponse<CardResponse>>(StatusCodes.Status200OK)]
    public async Task<PagedResponse<CardResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] DateOnly? expirationDateFrom = null,
        [FromQuery] DateOnly? expirationDateTo = null,
        CancellationToken ct = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync(ct);
        return await cardsService.ListAsync(userId, page, expirationDateFrom, expirationDateTo, ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<CardResponse> GetById(Guid id, CancellationToken ct = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync(ct);
        return await cardsService.GetAsync(userId, id, ct);
    }

    /// <summary>Creates a card for the authenticated user. The response never contains the full number or the PIN.</summary>
    [HttpPost]
    [ProducesResponseType<CardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CardResponse>> Create(CreateCardRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync(ct);
        var card = await cardsService.CreateAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    /// <summary>Fully replaces the editable fields of a card (PUT semantics: absent fields are rejected, except nickname which is cleared).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<CardResponse> Replace(Guid id, UpdateCardRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync(ct);
        return await cardsService.ReplaceAsync(userId, id, request, ct);
    }

    /// <summary>Partially updates a card (merge semantics: only fields present in the payload are applied).</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<CardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<CardResponse> Patch(Guid id, PatchCardRequest request, CancellationToken ct = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync(ct);
        return await cardsService.PatchAsync(userId, id, request, ct);
    }

    /// <summary>Removes a card (soft delete: it disappears from all regular queries).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var userId = await currentUser.GetRequiredUserIdAsync(ct);
        await cardsService.DeleteAsync(userId, id, ct);
        return NoContent();
    }
}
