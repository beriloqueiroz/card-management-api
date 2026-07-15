using Cards.Application.Abstractions;

namespace Cards.Api.ErrorHandling;

public sealed class CardAuditLogger(ILogger<CardAuditLogger> logger) : ICardAuditLogger
{
    public void PinRevealed(Guid userId, Guid cardId) =>
        logger.LogInformation("AUDIT card_pin_revealed userId={UserId} cardId={CardId}", userId, cardId);
}
