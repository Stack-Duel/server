using Npgsql;
using StackDuel.IntegrationTests.Infrastructure;

namespace StackDuel.IntegrationTests;

public abstract class ApiIntegrationTestBase
{
    private HttpClient? _client;

    protected HttpClient Client => _client ?? throw new InvalidOperationException("SetUp has not run yet.");

    protected TestDataSeeder Seeder
    {
        get => field ?? throw new InvalidOperationException("SetUp has not run yet.");
        private set;
    }

    [SetUp]
    public void SetUpClient()
    {
        _client = IntegrationTestEnvironment.Factory.CreateClient();
        Seeder = new TestDataSeeder(IntegrationTestEnvironment.ConnectionString);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        _client?.Dispose();
        _client = null;

        await using NpgsqlConnection connection = new(IntegrationTestEnvironment.ConnectionString);
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