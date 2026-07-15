using Npgsql;
using Testcontainers.PostgreSql;

namespace Cards.IntegrationTests.Support;

/// <summary>
/// One PostgreSQL container for the whole integration suite; each test class
/// gets its own database inside it, so suites stay isolated without paying
/// for one container each.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private int _databaseCounter;

    /// <summary>Creates a dedicated database (unique per call) and returns its connection string.</summary>
    public async Task<string> CreateDatabaseAsync(string name)
    {
        var databaseName = $"{name}_{Interlocked.Increment(ref _databaseCounter)}";

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {databaseName}", connection);
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName,
        }.ConnectionString;
    }
}

/// <summary>
/// Class fixture (one per test class): boots the API against its own fresh
/// database inside the shared container.
/// </summary>
public sealed class ApiFactoryFixture(PostgresContainerFixture postgres) : IAsyncLifetime
{
    public Api.CardsApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync() =>
        Factory = new Api.CardsApiFactory(await postgres.CreateDatabaseAsync("api_tests"));

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}
