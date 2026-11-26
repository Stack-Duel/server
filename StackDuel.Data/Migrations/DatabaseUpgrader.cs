using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using Npgsql;

namespace StackDuel.Data.Migrations;

public sealed class DatabaseUpgrader(string connectionString, IUpgradeLog log)
{
    private const string JournalSchema = "public";

    private static readonly string[] MaintenanceDatabases = ["postgres", "defaultdb", "template1"];

    public void EnsureDatabaseExists() => EnsureDatabaseExists(MaintenanceDatabases);

    public void EnsureDatabaseExists(IReadOnlyList<string> maintenanceDatabases)
    {
        if (DatabaseExists(connectionString))
            return;

        string databaseName =
            new NpgsqlConnectionStringBuilder(connectionString).Database
            ?? throw new InvalidOperationException("The connection string names no database to create.");

        foreach (string maintenanceDatabase in maintenanceDatabases)
        {
            string maintenanceConnectionString = WithDatabase(maintenanceDatabase);

            if (!DatabaseExists(maintenanceConnectionString))
                continue;

            Create(maintenanceConnectionString, databaseName);
            log.LogInformation("Created database {0}.", databaseName);
            return;
        }

        throw new InvalidOperationException(
            $"Cannot create '{databaseName}': none of {string.Join(", ", maintenanceDatabases)} "
                + "was reachable to connect to. Create the database by hand, or pass a connection "
                + "string for a database that already exists."
        );
    }

    private string WithDatabase(string databaseName) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName }.ConnectionString;

    private static bool DatabaseExists(string connectionString)
    {
        try
        {
            using NpgsqlConnection connection = new(connectionString);
            connection.Open();
            return true;
        }
        catch (PostgresException exception)
            when (exception.SqlState is PostgresErrorCodes.InvalidCatalogName)
        {
            return false;
        }
    }

    private static void Create(string maintenanceConnectionString, string databaseName)
    {
        using NpgsqlConnection connection = new(maintenanceConnectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
        command.ExecuteNonQuery();
    }

    public bool TryConnect(out string error) => Build(ScriptGroups.Schema).TryConnect(out error);

    public GroupStatus GetStatus(ScriptGroup group)
    {
        UpgradeEngine engine = Build(group);
        List<string> toRun = [.. engine.GetScriptsToExecute().Select(script => script.Name)];

        return new GroupStatus
        {
            Group = group,
            AppliedScripts = engine.GetExecutedScripts(),
            PendingScripts = [.. toRun.Where(name => !ScriptGroup.IsRepeatable(name))],
            RepeatableScripts = [.. toRun.Where(ScriptGroup.IsRepeatable)],
            OrphanedScripts = engine.GetExecutedButNotDiscoveredScripts(),
        };
    }

    public GroupUpgradeResult Upgrade(ScriptGroup group, Action<string>? onScriptExecuted = null)
    {
        UpgradeEngine engine = Build(group);

        if (onScriptExecuted is not null)
            engine.ScriptExecuted += (_, args) =>
            {
                if (args is ScriptExecutedEventArgs executed)
                    onScriptExecuted(executed.Script.Name);
            };

        DatabaseUpgradeResult result = engine.PerformUpgrade();
        IReadOnlyList<string> executed = [.. result.Scripts.Select(script => script.Name)];

        return result.Successful
            ? GroupUpgradeResult.Applied(group, executed)
            : GroupUpgradeResult.Failed(group, executed, result.Error);
    }

    public IReadOnlyList<string> MarkAsExecuted(ScriptGroup group)
    {
        UpgradeEngine engine = Build(group);
        IReadOnlyList<string> pending =
        [
            .. engine
                .GetScriptsToExecute()
                .Select(script => script.Name)
                .Where(name => !ScriptGroup.IsRepeatable(name)),
        ];

        foreach (string name in pending)
            engine.MarkAsExecuted(name);

        return pending;
    }

    public void DropEverything()
    {
        using NpgsqlConnection connection = new(connectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "DROP SCHEMA public CASCADE; CREATE SCHEMA public;";
        command.ExecuteNonQuery();
    }

    private UpgradeEngine Build(ScriptGroup group) =>
        DeployChanges
            .To.PostgresqlDatabase(new StandardConformingConnectionManager(connectionString))
            .WithScripts(EmbeddedScriptSource.Load(group))
            .JournalToPostgresqlTable(JournalSchema, group.JournalTable)
            .WithTransactionPerScript()
            .WithExecutionTimeout(TimeSpan.FromMinutes(5))
            .WithVariablesDisabled()
            .LogTo(log)
            .Build();
}