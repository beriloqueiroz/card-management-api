using System.Security.Claims;
using Cards.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Cards.Api.Auth;

/// <summary>
/// Bridges the JWT principal to the seed users table. The e-mail comes from
/// the token claims when present, otherwise from the IdP userinfo endpoint;
/// the resolved user id is cached per token subject to avoid repeated lookups.
/// </summary>
public sealed class CurrentUserProvider(
    IHttpContextAccessor httpContextAccessor,
    IUserRepository users,
    IUserInfoClient userInfo,
    IMemoryCache cache) : ICurrentUserProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Guid> GetRequiredUserIdAsync(CancellationToken ct = default)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No HTTP context available.");
        var principal = httpContext.User;

        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var cacheKey = subject is null ? null : $"current-user:{subject}";
        if (cacheKey is not null && cache.TryGetValue<Guid>(cacheKey, out var cached))
        {
            return cached;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(email))
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                email = await userInfo.GetEmailAsync(accessToken, ct);
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnknownUserException();
        }

        var user = await users.GetByEmailAsync(email, ct) ?? throw new UnknownUserException();
        if (cacheKey is not null)
        {
            cache.Set(cacheKey, user.Id, CacheDuration);
        }

        return user.Id;
    }
}
