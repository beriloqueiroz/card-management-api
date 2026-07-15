using Cards.Application.Abstractions;
using Cards.Infrastructure.Persistence;
using Cards.Infrastructure.Persistence.Repositories;
using Cards.Infrastructure.Persistence.Seeding;
using Cards.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CardsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("cards")));

        services.AddOptions<PinEncryptionOptions>()
            .BindConfiguration(PinEncryptionOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "PinEncryption:Key is required.")
            .ValidateOnStart();

        services.AddSingleton<IPinCipher, AesGcmPinCipher>();
        services.AddScoped<ICreditCardRepository, CreditCardRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
