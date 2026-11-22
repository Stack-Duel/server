using Data.Infrastructure;
using Spectre.Console;
using StackDuel.Infrastructure.Persistence.Migrations;
using System.CommandLine;

namespace Data.Commands;

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

            DatabaseUpgrader upgrader = new(
                ConnectionStringResolver.Resolve(parseResult.GetValue(SharedOptions.Connection)),
                new SpectreUpgradeLog(parseResult.GetValue(SharedOptions.Verbose))
            );

            upgrader.EnsureDatabaseExists();
            upgrader.DropEverything();
            AnsiConsole.MarkupLine("[grey]Dropped the public schema.[/]");

            List<ScriptGroup> groups = [.. ScriptGroups.All];

            if (parseResult.GetValue(dev))
                groups.Add(SeedScriptGroups.Dev);

            if (parseResult.GetValue(e2e))
                groups.Add(SeedScriptGroups.E2E);

            return UpgradeRunner.Run(upgrader, groups, dryRun: false);
        });

        return command;
    }
}