using StackDuel.Data.Migrations;
using StackDuel.IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;

namespace StackDuel.IntegrationTests;

/// <summary>
/// Owns the Postgres container and the API host for the whole assembly. xUnit builds one instance
/// per test collection, so every test class that joins <see cref="IntegrationTestCollection"/>
/// shares a single container instead of starting its own.
/// </summary>
public sealed class IntegrationTestEnvironment : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    private string? _connectionString;

    private StackDuelApiFactory? _factory;

    public string ConnectionString =>
        _connectionString
        ?? throw new InvalidOperationException("The Postgres test container has not been started yet.");

    public StackDuelApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("The API test factory has not been started yet.");

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17").Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);

        ApplySchema(_connectionString);

        _factory = new StackDuelApiFactory();
        _ = _factory.Services;

        await DatabaseReset.TruncateEverythingAsync(_connectionString);
    }

    private static void ApplySchema(string connectionString)
    {
        DatabaseUpgrader upgrader = new(connectionString, new SilentUpgradeLog());
        GroupUpgradeResult result = upgrader.Upgrade(ScriptGroups.Schema);

        if (!result.Succeeded)
            throw new InvalidOperationException("Applying the schema scripts to the test container failed.", result.Error);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        if (_container is not null)
            await _container.DisposeAsync();
    }
}