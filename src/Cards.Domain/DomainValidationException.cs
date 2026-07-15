namespace Cards.Domain;

/// <summary>
/// Raised when input violates a domain invariant. Messages must never echo
/// sensitive values (card number, PIN) back to the caller.
/// </summary>
public sealed class DomainValidationException(string message) : Exception(message);
