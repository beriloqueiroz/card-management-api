namespace Cards.Api.Auth;

/// <summary>Token is valid but its subject does not match any user in the database. Maps to 403.</summary>
public sealed class UnknownUserException() : Exception("Authenticated user is not registered.");
