using StackDuel.IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;

namespace StackDuel.IntegrationTests;

[SetUpFixture]
public sealed class IntegrationTestEnvironment
{
    private static PostgreSqlContainer? _container;

    private static string? _connectionString;

    private static StackDuelApiFactory? _factory;

    public static string ConnectionString =>
        _connectionString
        ?? throw new InvalidOperationException("The Postgres test container has not been started yet.");

    public static StackDuelApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("The API test factory has not been started yet.");

    [OneTimeSetUp]
    public async Task StartEnvironmentAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17").Build();
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);

        _factory = new StackDuelApiFactory();
        _ = _factory.Services;
    }

    [OneTimeTearDown]
    public async Task StopEnvironmentAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        if (_container is not null)
            await _container.DisposeAsync();
    }
}