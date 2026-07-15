using System.Net;
using System.Text;
using Cards.Api.Auth;

namespace Cards.UnitTests.Api;

public class ZitadelUserInfoClientTests
{
    /// <summary>Fakes the network layer: records the outgoing request and returns a canned response.</summary>
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static (ZitadelUserInfoClient Client, StubHandler Handler) Create(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://auth.localhost:8080/") };
        return (new ZitadelUserInfoClient(httpClient), handler);
    }

    [Fact]
    public async Task GetEmailAsync_CallsUserinfoWithBearerTokenAndParsesEmail()
    {
        var (client, handler) = Create(HttpStatusCode.OK,
            """{"sub":"123","email":"mariana.alves@cardcorp.test","email_verified":true}""");

        var email = await client.GetEmailAsync("the-access-token");

        Assert.Equal("mariana.alves@cardcorp.test", email);
        Assert.Equal("http://auth.localhost:8080/oidc/v1/userinfo", handler.Request!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("the-access-token", handler.Request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetEmailAsync_ReturnsNullWhenIdpRejectsTheToken()
    {
        var (client, _) = Create(HttpStatusCode.Unauthorized, """{"error":"invalid_token"}""");

        Assert.Null(await client.GetEmailAsync("expired-token"));
    }

    [Fact]
    public async Task GetEmailAsync_ReturnsNullWhenEmailClaimIsAbsent()
    {
        var (client, _) = Create(HttpStatusCode.OK, """{"sub":"123"}""");

        Assert.Null(await client.GetEmailAsync("token-without-email-scope"));
    }
}
