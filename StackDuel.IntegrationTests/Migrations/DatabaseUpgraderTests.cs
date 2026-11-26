using Npgsql;
using StackDuel.Data.Migrations;
using StackDuel.IntegrationTests.Infrastructure;

namespace StackDuel.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.Name)]
public sealed class DatabaseUpgraderTests(IntegrationTestEnvironment environment)
{
    private sealed record FreshDatabase(DatabaseUpgrader Upgrader, string ConnectionString);

    private FreshDatabase CreateFreshDatabase()
    {
        NpgsqlConnectionStringBuilder builder = new(environment.ConnectionString)
        {
            Database = $"upgrader_{Guid.NewGuid():N}",
        };

        DatabaseUpgrader upgrader = new(builder.ConnectionString, new SilentUpgradeLog());
        upgrader.EnsureDatabaseExists();

        return new FreshDatabase(upgrader, builder.ConnectionString);
    }

    [Fact]
    public void AFreshDatabaseIsReachableButHasNothingApplied()
    {
        DatabaseUpgrader upgrader = CreateFreshDatabase().Upgrader;

        Assert.True(upgrader.TryConnect(out string error), error);

        GroupStatus status = upgrader.GetStatus(ScriptGroups.Schema);

        Assert.False(status.IsUpToDate);
        Assert.Empty(status.AppliedScripts);
        Assert.NotEmpty(status.PendingScripts);
        Assert.Empty(status.OrphanedScripts);
    }

    [Fact]
    public void UpgradingAppliesEveryPendingScriptAndReportsEachOne()
    {
        DatabaseUpgrader upgrader = CreateFreshDatabase().Upgrader;
        List<string> reported = [];

        GroupUpgradeResult result = upgrader.Upgrade(ScriptGroups.Schema, reported.Add);

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.NotEmpty(result.ExecutedScripts);
        Assert.Equal(result.ExecutedScripts, reported);

        GroupStatus status = upgrader.GetStatus(ScriptGroups.Schema);

        Assert.True(status.IsUpToDate);
        Assert.Empty(status.PendingScripts);
        Assert.Equal(result.ExecutedScripts.Count, status.AppliedScripts.Count);
    }

    [Fact]
    public void UpgradingTwiceRunsNothingTheSecondTime()
    {
        DatabaseUpgrader upgrader = CreateFreshDatabase().Upgrader;

        Assert.True(upgrader.Upgrade(ScriptGroups.Schema).Succeeded);
        GroupUpgradeResult second = upgrader.Upgrade(ScriptGroups.Schema);

        Assert.True(second.Succeeded, second.Error?.Message);
        Assert.Empty(second.ExecutedScripts);
    }

    [Fact]
    public async Task MarkingAsExecutedJournalsTheScriptsWithoutRunningThem()
    {
        FreshDatabase database = CreateFreshDatabase();

        IReadOnlyList<string> marked = database.Upgrader.MarkAsExecuted(ScriptGroups.Schema);

        Assert.NotEmpty(marked);

        GroupStatus status = database.Upgrader.GetStatus(ScriptGroups.Schema);

        Assert.Empty(status.PendingScripts);
        Assert.Equal(marked.Count, status.AppliedScripts.Count);

        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.problems') IS NULL";

        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public void DroppingEverythingLeavesTheGroupPendingAgain()
    {
        DatabaseUpgrader upgrader = CreateFreshDatabase().Upgrader;

        Assert.True(upgrader.Upgrade(ScriptGroups.Schema).Succeeded);
        Assert.True(upgrader.GetStatus(ScriptGroups.Schema).IsUpToDate);

        upgrader.DropEverything();

        GroupStatus status = upgrader.GetStatus(ScriptGroups.Schema);

        Assert.False(status.IsUpToDate);
        Assert.Empty(status.AppliedScripts);
    }

    [Fact]
    public void EveryGroupAppliesInOrder()
    {
        DatabaseUpgrader upgrader = CreateFreshDatabase().Upgrader;

        foreach (ScriptGroup group in ScriptGroups.All)
        {
            GroupUpgradeResult result = upgrader.Upgrade(group);

            Assert.True(result.Succeeded, $"{group.Name}: {result.Error?.Message}");
        }

        Assert.All(ScriptGroups.All, group => Assert.True(upgrader.GetStatus(group).IsUpToDate));
    }
    private DatabaseUpgrader UpgraderForAnUncreatedDatabase()
    {
        NpgsqlConnectionStringBuilder builder = new(environment.ConnectionString)
        {
            Database = $"upgrader_{Guid.NewGuid():N}",
        };

        return new DatabaseUpgrader(builder.ConnectionString, new SilentUpgradeLog());
    }

    [Fact]
    public void CreatingSkipsTheMaintenanceDatabasesItCannotReach()
    {
        DatabaseUpgrader upgrader = UpgraderForAnUncreatedDatabase();

        upgrader.EnsureDatabaseExists(["no_such_maintenance_database", "postgres"]);

        Assert.True(upgrader.TryConnect(out string error), error);
        Assert.NotEmpty(upgrader.GetStatus(ScriptGroups.Schema).PendingScripts);
    }

    [Fact]
    public void CreatingIsAnEmptyActWhenTheDatabaseIsAlreadyThere()
    {
        DatabaseUpgrader upgrader = CreateFreshDatabase().Upgrader;

        upgrader.EnsureDatabaseExists(["no_such_maintenance_database"]);

        Assert.True(upgrader.TryConnect(out string error), error);
    }

    [Fact]
    public void CreatingSaysWhichMaintenanceDatabasesItTried()
    {
        DatabaseUpgrader upgrader = UpgraderForAnUncreatedDatabase();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => upgrader.EnsureDatabaseExists(["no_such_database", "also_missing"])
        );

        Assert.Contains("no_such_database, also_missing", exception.Message, StringComparison.Ordinal);
    }
}