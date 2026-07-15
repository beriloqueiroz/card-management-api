using Cards.Api.Auth;
using Cards.Api.ErrorHandling;
using Cards.Application.Abstractions;
using Cards.Application.Cards;
using Cards.Infrastructure;
using Cards.Infrastructure.Persistence;
using Cards.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Values produced by the zitadel-bootstrap container land in files (ids are
// generated at first init), so config accepts either a literal or a file path.
string? FromConfigOrFile(string key)
{
    var literal = builder.Configuration[key];
    if (!string.IsNullOrWhiteSpace(literal))
    {
        return literal;
    }

    var path = builder.Configuration[$"{key}File"];
    return path is not null && File.Exists(path) ? File.ReadAllText(path).Trim() : null;
}

var authority = builder.Configuration["Auth:Authority"];
var audience = FromConfigOrFile("Auth:Audience");
var swaggerClientId = FromConfigOrFile("Auth:SwaggerClientId");

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

// Application services (composition root keeps Cards.Application framework-free).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICardsService, CardsService>();
builder.Services.AddScoped<ICardAuditLogger, CardAuditLogger>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

if (string.IsNullOrEmpty(authority))
{
    builder.Services.AddSingleton<IUserInfoClient, NullUserInfoClient>();
}
else
{
    builder.Services.AddHttpClient<IUserInfoClient, ZitadelUserInfoClient>(client =>
        client.BaseAddress = new Uri(authority.TrimEnd('/') + "/"));
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        // The local IdP runs over plain HTTP inside docker compose.
        options.RequireHttpsMetadata = builder.Configuration.GetValue("Auth:RequireHttpsMetadata", false);
        options.TokenValidationParameters.ValidateAudience = !string.IsNullOrEmpty(audience);
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cards API",
        Version = "v1",
        Description = "API REST de gestão de cartões de crédito do usuário autenticado. "
            + "Números de cartão aparecem sempre mascarados; o PIN só existe no endpoint exclusivo.",
    });

    foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
    {
        options.IncludeXmlComments(xml);
    }

    if (!string.IsNullOrEmpty(authority))
    {
        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Login via ZITADEL (authorization code + PKCE). Tokens expiram em 30 minutos.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority}/oauth/v2/authorize"),
                    TokenUrl = new Uri($"{authority}/oauth/v2/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "OpenID Connect",
                        ["profile"] = "Perfil básico",
                        ["email"] = "E-mail (usado para identificar o usuário)",
                        ["offline_access"] = "Refresh token (rotação/renovação)",
                    },
                },
            },
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
                },
                ["openid", "profile", "email", "offline_access"]
            },
        });
    }
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(ui =>
{
    if (!string.IsNullOrEmpty(swaggerClientId))
    {
        ui.OAuthClientId(swaggerClientId);
        ui.OAuthUsePkce();
        ui.OAuthScopes("openid", "profile", "email", "offline_access");
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CardsDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.Run();

public partial class Program;
