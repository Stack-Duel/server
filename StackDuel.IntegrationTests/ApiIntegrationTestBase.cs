using Npgsql;
using StackDuel.IntegrationTests.Infrastructure;

namespace StackDuel.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public abstract class ApiIntegrationTestBase(IntegrationTestEnvironment environment) : IAsyncLifetime
{
    private HttpClient? _client;

    protected HttpClient Client => _client ?? throw new InvalidOperationException("InitializeAsync has not run yet.");

    protected TestDataSeeder Seeder
    {
        get => field ?? throw new InvalidOperationException("InitializeAsync has not run yet.");
        private set;
    }

    public Task InitializeAsync()
    {
        _client = environment.Factory.CreateClient();
        Seeder = new TestDataSeeder(environment.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _client = null;

        await using NpgsqlConnection connection = new(environment.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            TRUNCATE TABLE
                user_language_preferences,
                user_groups,
                group_roles,
                role_permissions,
                security_restrictions,
                users,
                groups,
                roles,
                permissions
            RESTART IDENTITY CASCADE
            """;
        await command.ExecuteNonQueryAsync();
    }

    protected static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string requestUri, string sub)
    {
        HttpRequestMessage request = new(method, requestUri);
        request.Headers.Add(TestAuthHandler.SubHeaderName, sub);
        return request;
    }
}