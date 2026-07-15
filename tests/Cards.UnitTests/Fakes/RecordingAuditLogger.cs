using Cards.Application.Abstractions;

namespace Cards.UnitTests.Fakes;

public sealed class RecordingAuditLogger : ICardAuditLogger
{
    public List<(Guid UserId, Guid CardId)> PinReveals { get; } = [];

    public void PinRevealed(Guid userId, Guid cardId) => PinReveals.Add((userId, cardId));
}
