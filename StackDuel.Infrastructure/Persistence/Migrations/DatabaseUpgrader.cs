using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using Npgsql;

namespace StackDuel.Infrastructure.Persistence.Migrations;

public sealed class DatabaseUpgrader(string connectionString, IUpgradeLog log)
{
    private const string JournalSchema = "public";

    public void EnsureDatabaseExists() => EnsureDatabase.For.PostgresqlDatabase(connectionString);

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