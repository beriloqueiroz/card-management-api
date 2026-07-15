using System.Net;
using System.Text;
using System.Text.Json;
using Cards.IntegrationTests.Api;
using Cards.IntegrationTests.Support;

namespace Cards.IntegrationTests.E2E;

/// <summary>
/// End-to-end journey over the real API using the literal JSON payloads from
/// the challenge PDF as executable specs. Runs on its own database (shared
/// container) so the seed counts stay pristine. Auth uses the test scheme —
/// the ZITADEL leg of the flow is exercised manually via Swagger.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CardsFlowE2eTests(ApiFactoryFixture fixture) : IClassFixture<ApiFactoryFixture>
{
    private const string Mariana = "mariana.alves@cardcorp.test";

    private static StringContent Json(string payload) =>
        new(payload, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task FullCardLifecycle_FollowingThePdfSpecs()
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, Mariana);

        // 4.2 Criação de cartão — exact payload from the PDF.
        var createResponse = await client.PostAsync("/api/cards", Json("""
            {
              "cardholderName": "MARIANA ALVES",
              "nickname": "Cartão Eventos",
              "brand": "VISA",
              "cardNumber": "5321123412345336",
              "expirationDate": "2029-12-31",
              "creditLimit": 6500.00,
              "pin": "1234"
            }
            """));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadJson(createResponse);
        var cardId = created.GetProperty("id").GetString();
        Assert.Equal("5321 **** **** 5336", created.GetProperty("maskedNumber").GetString());
        Assert.DoesNotContain("5321123412345336", created.GetRawText());

        // 4.1 Listagem — minimal paginated shape from the PDF; the new card is
        // the most recent, and Mariana now has 13 cards (12 seeded + 1).
        var list = await ReadJson(await client.GetAsync("/api/cards"));
        Assert.Equal(1, list.GetProperty("page").GetInt32());
        Assert.Equal(10, list.GetProperty("pageSize").GetInt32());
        Assert.Equal(13, list.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, list.GetProperty("totalPages").GetInt32());
        Assert.Equal(cardId, list.GetProperty("items")[0].GetProperty("id").GetString());

        // 4.3 Atualização completa (PUT) — exact payload from the PDF.
        var putResponse = await client.PutAsync($"/api/cards/{cardId}", Json("""
            {
              "cardholderName": "MARIANA ALVES",
              "nickname": "Principal Atualizado",
              "brand": "VISA",
              "cardNumber": "5321123412345336",
              "expirationDate": "2029-01-31",
              "creditLimit": 14000.00,
              "status": "ACTIVE",
              "pin": "9876"
            }
            """));
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var afterPut = await ReadJson(putResponse);
        Assert.Equal("Principal Atualizado", afterPut.GetProperty("nickname").GetString());
        Assert.Equal("2029-01-31", afterPut.GetProperty("expirationDate").GetString());
        Assert.Equal(14000.00m, afterPut.GetProperty("creditLimit").GetDecimal());

        // 4.4 Atualização parcial (PATCH) — exact payload from the PDF.
        var patchResponse = await client.PatchAsync($"/api/cards/{cardId}", Json("""
            {
              "nickname": "Uso diário",
              "creditLimit": 15500.00
            }
            """));
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var afterPatch = await ReadJson(patchResponse);
        Assert.Equal("Uso diário", afterPatch.GetProperty("nickname").GetString());
        Assert.Equal(15500.00m, afterPatch.GetProperty("creditLimit").GetDecimal());
        Assert.Equal("MARIANA ALVES", afterPatch.GetProperty("cardholderName").GetString());

        // 4.6 Consulta exclusiva da senha — returns the PIN set by the PUT.
        var pinResponse = await client.GetAsync($"/api/cards/{cardId}/pin");
        Assert.Equal(HttpStatusCode.OK, pinResponse.StatusCode);
        Assert.Contains("no-store", pinResponse.Headers.CacheControl?.ToString());
        Assert.Equal("9876", (await ReadJson(pinResponse)).GetProperty("pin").GetString());

        // 4.5 Remoção — the card leaves every regular query.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/cards/{cardId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/cards/{cardId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/cards/{cardId}/pin")).StatusCode);

        var finalList = await ReadJson(await client.GetAsync("/api/cards"));
        Assert.Equal(12, finalList.GetProperty("totalItems").GetInt32());
    }
}
