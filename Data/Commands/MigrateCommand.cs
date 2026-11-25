using Data.Infrastructure;
using StackDuel.Infrastructure.Persistence.Migrations;
using System.CommandLine;

namespace Data.Commands;

internal static class MigrateCommand
{
    public static Command Create()
    {
        Option<bool> dryRun = new("--dry-run") { Description = "List the pending scripts without running them." };
        Option<bool> createDatabase = new("--create-database")
        {
            Description = "Create the database first if it does not exist yet.",
        };

        Command command = new("migrate", "Apply the schema and the data the application needs to run.");
        SharedOptions.AddTo(command);
        command.Options.Add(SharedOptions.Groups);
        command.Options.Add(dryRun);
        command.Options.Add(createDatabase);

        command.SetAction(parseResult =>
        {
            if (
                !GroupSelection.TryResolve(
                    parseResult.GetValue(SharedOptions.Groups) ?? [],
                    ScriptGroups.All,
                    out IReadOnlyList<ScriptGroup> groups
                )
            )
                return UpgradeRunner.Failure;

            DatabaseUpgrader upgrader = new(
                ConnectionStringResolver.Resolve(parseResult.GetValue(SharedOptions.Connection)),
                new SpectreUpgradeLog(parseResult.GetValue(SharedOptions.Verbose))
            );

            if (parseResult.GetValue(createDatabase))
                upgrader.EnsureDatabaseExists();

            return UpgradeRunner.Run(upgrader, groups, parseResult.GetValue(dryRun));
        });

        return command;
    }
}