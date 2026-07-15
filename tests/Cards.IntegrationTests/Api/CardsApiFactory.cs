using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cards.IntegrationTests.Api;

/// <summary>
/// Boots the real API (migrations + seed included) against the database
/// whose connection string is provided, with authentication swapped for the
/// test scheme.
/// </summary>
public sealed class CardsApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:cards", connectionString);
        builder.UseSetting("Auth:Authority", string.Empty);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}
