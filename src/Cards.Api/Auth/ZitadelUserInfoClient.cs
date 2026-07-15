using System.Net.Http.Headers;
using System.Text.Json;

namespace Cards.Api.Auth;

public sealed class ZitadelUserInfoClient(HttpClient httpClient) : IUserInfoClient
{
    public async Task<string?> GetEmailAsync(string accessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "oidc/v1/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return json.RootElement.TryGetProperty("email", out var email) ? email.GetString() : null;
    }
}
