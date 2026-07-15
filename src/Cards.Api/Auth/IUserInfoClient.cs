namespace Cards.Api.Auth;

/// <summary>
/// Fallback identity source: ZITADEL JWT access tokens carry `sub` but not
/// necessarily `email`, so when the claim is absent we ask the IdP's
/// userinfo endpoint with the caller's own token.
/// </summary>
public interface IUserInfoClient
{
    Task<string?> GetEmailAsync(string accessToken, CancellationToken ct = default);
}

/// <summary>Used when no Authority is configured (tests): claims are the only source.</summary>
public sealed class NullUserInfoClient : IUserInfoClient
{
    public Task<string?> GetEmailAsync(string accessToken, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
}
