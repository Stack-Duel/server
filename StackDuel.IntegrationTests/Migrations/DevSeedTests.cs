using Data;
using Data.Migrations;
using Data.Seeding;
using Npgsql;
using StackDuel.IntegrationTests.Infrastructure;

namespace StackDuel.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.Name)]
public sealed class DevSeedTests(IntegrationTestEnvironment environment)
{
    private string SeedAFreshDatabase()
    {
        NpgsqlConnectionStringBuilder builder = new(environment.ConnectionString)
        {
            Database = $"devseed_{Guid.NewGuid():N}",
        };

        DatabaseUpgrader upgrader = new(builder.ConnectionString, new SilentUpgradeLog());
        upgrader.EnsureDatabaseExists();

        foreach (ScriptGroup group in ScriptGroups.All)
        {
            GroupUpgradeResult result = upgrader.Upgrade(group);
            Assert.True(result.Succeeded, $"{group.Name}: {result.Error?.Message}");
        }

        Assert.True(FileBundleSeeder.EnsureChinookBundle(builder.ConnectionString));

        GroupUpgradeResult dev = upgrader.Upgrade(SeedScriptGroups.Dev);
        Assert.True(dev.Succeeded, dev.Error?.Message);

        return builder.ConnectionString;
    }

    private static async Task<long> ScalarAsync(string connectionString, string sql)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sql;

        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task TheDevSeedProducesAWorkableCatalog()
    {
        string connectionString = SeedAFreshDatabase();

        Assert.Equal(4, await ScalarAsync(connectionString, "SELECT count(*) FROM problems"));
        Assert.Equal(6, await ScalarAsync(connectionString, "SELECT count(*) FROM problem_setups"));
        Assert.Equal(4, await ScalarAsync(connectionString, "SELECT count(*) FROM test_suites"));
        Assert.Equal(7, await ScalarAsync(connectionString, "SELECT count(*) FROM test_cases"));
        Assert.Equal(
            7,
            await ScalarAsync(connectionString, "SELECT count(*) FROM test_case_expected_outputs")
        );
    }

    [Fact]
    public async Task EverySeededSetupResolvesItsLanguageTrackAndPipeline()
    {
        string connectionString = SeedAFreshDatabase();

        Assert.Equal(
            0,
            await ScalarAsync(
                connectionString,
                """
                SELECT count(*) FROM problem_setups s
                LEFT JOIN language_versions v ON v.id = s.language_version_id
                LEFT JOIN execution_pipelines p ON p.id = s.pipeline_id
                WHERE v.id IS NULL OR p.id IS NULL
                """
            )
        );

        Assert.Equal(
            0,
            await ScalarAsync(
                connectionString,
                "SELECT count(*) FROM problems p LEFT JOIN tracks t ON t.id = p.track_id WHERE t.id IS NULL"
            )
        );
    }

    [Fact]
    public async Task TheSqlProblemIsBoundToTheChinookBundle()
    {
        string connectionString = SeedAFreshDatabase();

        Assert.Equal(
            1,
            await ScalarAsync(
                connectionString,
                """
                SELECT count(*) FROM problem_setups s
                JOIN additional_file_bundles b ON b.id = s.additional_file_bundle_id
                WHERE b.name = 'chinook-sqlite-db'
                """
            )
        );

        Assert.Equal(
            335000,
            await ScalarAsync(
                connectionString,
                "SELECT length(content) FROM additional_file_bundles WHERE name = 'chinook-sqlite-db'"
            )
        );
    }

    [Fact]
    public async Task SeedingTwiceChangesNothing()
    {
        string connectionString = SeedAFreshDatabase();

        DatabaseUpgrader upgrader = new(connectionString, new SilentUpgradeLog());

        Assert.False(FileBundleSeeder.EnsureChinookBundle(connectionString));
        Assert.Empty(upgrader.Upgrade(SeedScriptGroups.Dev).ExecutedScripts);
        Assert.Equal(4, await ScalarAsync(connectionString, "SELECT count(*) FROM problems"));
    }
}