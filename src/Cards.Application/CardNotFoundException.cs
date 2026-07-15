namespace Cards.Application;

/// <summary>
/// Raised when a card does not exist for the authenticated user. Cards owned
/// by other users raise the same exception, so a caller cannot distinguish
/// "not mine" from "does not exist".
/// </summary>
public sealed class CardNotFoundException() : Exception("Card not found.");
