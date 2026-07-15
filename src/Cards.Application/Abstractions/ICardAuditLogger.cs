namespace Cards.Application.Abstractions;

/// <summary>
/// Audit trail for sensitive flows. Implementations must record identifiers
/// and timestamps only — never card numbers or PINs.
/// </summary>
public interface ICardAuditLogger
{
    void PinRevealed(Guid userId, Guid cardId);
}
