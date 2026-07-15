namespace Cards.Api.Auth;

public interface ICurrentUserProvider
{
    /// <summary>Resolves the seed user matching the authenticated principal, or throws <see cref="UnknownUserException"/>.</summary>
    Task<Guid> GetRequiredUserIdAsync(CancellationToken ct = default);
}
