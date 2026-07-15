using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cards.IntegrationTests.Support;

namespace Cards.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class CardsApiTests(ApiFactoryFixture fixture) : IClassFixture<ApiFactoryFixture>
{
    private const string Mariana = "mariana.alves@cardcorp.test";
    private const string Rafael = "rafael.souza@cardcorp.test";
    private const string MarianaPrincipalCardId = "00000000-0000-4000-9000-000000000001";

    private HttpClient ClientFor(string? email)
    {
        var client = fixture.Factory.CreateClient();
        if (email is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, email);
        }

        return client;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    // --- auth ----------------------------------------------------------------

    [Fact]
    public async Task Endpoints_RequireAuthentication()
    {
        var anonymous = ClientFor(null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/cards")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync($"/api/cards/{MarianaPrincipalCardId}/pin")).StatusCode);
    }

    // --- listing ----------------------------------------------------------------

    [Fact]
    public async Task List_PaginatesInFixedBlocksOfTenNewestFirst()
    {
        var client = ClientFor(Mariana);

        var page1 = await ReadJson(await client.GetAsync("/api/cards?page=1"));
        var page2 = await ReadJson(await client.GetAsync("/api/cards?page=2"));

        Assert.Equal(1, page1.GetProperty("page").GetInt32());
        Assert.Equal(10, page1.GetProperty("pageSize").GetInt32());
        Assert.Equal(12, page1.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, page1.GetProperty("totalPages").GetInt32());
        Assert.Equal(10, page1.GetProperty("items").GetArrayLength());
        Assert.Equal(2, page2.GetProperty("items").GetArrayLength());

        // Newest first: seed's most recent card is "Principal" (2026-06-30).
        Assert.Equal("Principal", page1.GetProperty("items")[0].GetProperty("nickname").GetString());
    }

    [Fact]
    public async Task List_FiltersByExpirationPeriodAndMasksNumbers()
    {
        var client = ClientFor(Mariana);

        var result = await ReadJson(await client.GetAsync(
            "/api/cards?expirationDateFrom=2028-01-01&expirationDateTo=2028-06-30"));

        Assert.Equal(6, result.GetProperty("totalItems").GetInt32());
        foreach (var item in result.GetProperty("items").EnumerateArray())
        {
            Assert.Matches(@"^\d{4} \*{4} \*{4} \d{4}$", item.GetProperty("maskedNumber").GetString());
            Assert.False(item.TryGetProperty("cardNumber", out _));
            Assert.False(item.TryGetProperty("pin", out _));
        }
    }

    [Fact]
    public async Task List_RejectsInvalidPageAndInvertedPeriod()
    {
        var client = ClientFor(Mariana);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/cards?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(
            "/api/cards?expirationDateFrom=2029-01-01&expirationDateTo=2028-01-01")).StatusCode);
    }

    // --- get by id / ownership ---------------------------------------------------

    [Fact]
    public async Task GetById_HidesOtherUsersCardsBehind404()
    {
        Assert.Equal(HttpStatusCode.OK,
            (await ClientFor(Mariana).GetAsync($"/api/cards/{MarianaPrincipalCardId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await ClientFor(Rafael).GetAsync($"/api/cards/{MarianaPrincipalCardId}")).StatusCode);
    }

    // --- create -------------------------------------------------------------------

    [Fact]
    public async Task Post_CreatesCardWithoutEchoingSensitiveData()
    {
        var client = ClientFor(Rafael);

        var response = await client.PostAsJsonAsync("/api/cards", new
        {
            cardholderName = "RAFAEL SOUZA",
            nickname = "Cartão Novo",
            brand = "VISA",
            cardNumber = "4929123412347777",
            expirationDate = "2031-12-31",
            creditLimit = 5000.00,
            pin = "4321",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await ReadJson(response);
        Assert.Equal("4929 **** **** 7777", body.GetProperty("maskedNumber").GetString());
        Assert.Equal("ACTIVE", body.GetProperty("status").GetString());
        var raw = body.GetRawText();
        Assert.DoesNotContain("4929123412347777", raw);
        Assert.DoesNotContain("4321", raw);
    }

    [Theory]
    [InlineData("{\"nickname\":\"sem obrigatorios\"}")] // missing required fields
    [InlineData("{\"cardholderName\":\"X\",\"brand\":\"VISA\",\"cardNumber\":\"4111111111111111\",\"expirationDate\":\"2031-12-31\",\"creditLimit\":-1,\"pin\":\"1234\"}")] // negative limit
    [InlineData("{\"cardholderName\":\"X\",\"brand\":\"VISA\",\"cardNumber\":\"4111111111111111\",\"expirationDate\":\"2031-12-31\",\"creditLimit\":10,\"pin\":\"1234\",\"status\":\"EXPIRED\"}")] // invalid status
    public async Task Post_RejectsInvalidPayloadsWithProblemDetails(string payload)
    {
        var client = ClientFor(Rafael);

        var response = await client.PostAsync("/api/cards",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJson(response);
        Assert.True(body.TryGetProperty("title", out _)); // RFC 7807 shape
    }

    // --- update ----------------------------------------------------------------------

    [Fact]
    public async Task PutAndPatch_UpdateEditableFields()
    {
        var client = ClientFor(Rafael);
        var created = await ReadJson(await client.PostAsJsonAsync("/api/cards", new
        {
            cardholderName = "RAFAEL SOUZA",
            nickname = "Para Editar",
            brand = "VISA",
            cardNumber = "4024007112345678",
            expirationDate = "2030-06-30",
            creditLimit = 1000.00,
            pin = "1111",
        }));
        var id = created.GetProperty("id").GetString();

        var putResponse = await client.PutAsJsonAsync($"/api/cards/{id}", new
        {
            cardholderName = "RAFAEL S SOUZA",
            nickname = "Editado",
            brand = "MASTERCARD",
            cardNumber = "5412345678901234",
            expirationDate = "2031-01-31",
            creditLimit = 2000.00,
            status = "BLOCKED",
            pin = "2222",
        });
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var afterPut = await ReadJson(putResponse);
        Assert.Equal("5412 **** **** 1234", afterPut.GetProperty("maskedNumber").GetString());
        Assert.Equal("BLOCKED", afterPut.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, afterPut.GetProperty("updatedAt").ValueKind);

        var patchResponse = await client.PatchAsJsonAsync($"/api/cards/{id}", new
        {
            nickname = "Uso diário",
            creditLimit = 15500.00,
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var afterPatch = await ReadJson(patchResponse);
        Assert.Equal("Uso diário", afterPatch.GetProperty("nickname").GetString());
        Assert.Equal(15500.00m, afterPatch.GetProperty("creditLimit").GetDecimal());
        Assert.Equal("RAFAEL S SOUZA", afterPatch.GetProperty("cardholderName").GetString()); // untouched
        Assert.Equal("5412 **** **** 1234", afterPatch.GetProperty("maskedNumber").GetString()); // untouched

        var emptyPatch = await client.PatchAsJsonAsync($"/api/cards/{id}", new { });
        Assert.Equal(HttpStatusCode.BadRequest, emptyPatch.StatusCode);
    }

    // --- delete -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_RemovesFromRegularQueries()
    {
        var client = ClientFor(Rafael);
        var created = await ReadJson(await client.PostAsJsonAsync("/api/cards", new
        {
            cardholderName = "RAFAEL SOUZA",
            nickname = "Para Excluir",
            brand = "ELO",
            cardNumber = "6505123412340001",
            expirationDate = "2030-06-30",
            creditLimit = 500.00,
            pin = "5555",
        }));
        var id = created.GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/cards/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/cards/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/cards/{id}")).StatusCode);
    }

    // --- PIN --------------------------------------------------------------------------

    [Fact]
    public async Task PinEndpoint_ReturnsOriginalPinOnlyToOwnerWithNoStoreHeaders()
    {
        var owner = ClientFor(Mariana);
        var response = await owner.GetAsync($"/api/cards/{MarianaPrincipalCardId}/pin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        var body = await ReadJson(response);
        Assert.Equal("1234", body.GetProperty("pin").GetString()); // documented seed PIN

        Assert.Equal(HttpStatusCode.NotFound,
            (await ClientFor(Rafael).GetAsync($"/api/cards/{MarianaPrincipalCardId}/pin")).StatusCode);
    }

    [Fact]
    public async Task PinNeverAppearsInRegularCardResponses()
    {
        var client = ClientFor(Mariana);

        var list = await (await client.GetAsync("/api/cards")).Content.ReadAsStringAsync();
        var single = await (await client.GetAsync($"/api/cards/{MarianaPrincipalCardId}")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("\"pin\"", list);
        Assert.DoesNotContain("pinEncrypted", list);
        Assert.DoesNotContain("\"pin\"", single);
        Assert.DoesNotContain("pinEncrypted", single);
    }
}
