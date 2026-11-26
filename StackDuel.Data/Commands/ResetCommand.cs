using Spectre.Console;
using StackDuel.Data.Infrastructure;
using StackDuel.Data.Migrations;
using System.CommandLine;

namespace StackDuel.Data.Commands;

internal static class ResetCommand
{
    public static Command Create()
    {
        Option<bool> force = new("--force") { Description = "Required. Confirms that losing every row is intended." };
        Option<bool> dev = new("--dev") { Description = "Apply the local sample data after migrating." };
        Option<bool> e2e = new("--e2e") { Description = "Apply the Playwright fixtures after migrating." };

        Command command = new("reset", "Drop every table, then migrate from scratch. Development databases only.");
        SharedOptions.AddTo(command);
        command.Options.Add(force);
        command.Options.Add(dev);
        command.Options.Add(e2e);

        command.SetAction(parseResult =>
        {
            if (!parseResult.GetValue(force))
            {
                AnsiConsole.MarkupLine("[red]reset drops every table.[/] Pass --force if that is what you want.");
                return UpgradeRunner.Failure;
            }

            string connectionString = ConnectionStringResolver.Resolve(
                parseResult.GetValue(SharedOptions.Connection)
            );

            DatabaseUpgrader upgrader = new(
                connectionString,
                new SpectreUpgradeLog(parseResult.GetValue(SharedOptions.Verbose))
            );

            upgrader.EnsureDatabaseExists();
            upgrader.DropEverything();
            AnsiConsole.MarkupLine("[grey]Dropped the public schema.[/]");

            int schemaResult = UpgradeRunner.Run(upgrader, [.. ScriptGroups.All], dryRun: false);

            if (schemaResult != UpgradeRunner.Success)
                return schemaResult;

            List<ScriptGroup> seedGroups = [];

            if (parseResult.GetValue(dev))
                seedGroups.Add(SeedScriptGroups.Dev);

            if (parseResult.GetValue(e2e))
                seedGroups.Add(SeedScriptGroups.E2E);

            if (seedGroups.Count == 0)
                return UpgradeRunner.Success;

            if (parseResult.GetValue(dev))
                ChinookBundleStep.Ensure(connectionString);

            return UpgradeRunner.Run(upgrader, seedGroups, dryRun: false);
        });

        return command;
    }
}