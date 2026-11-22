using System.CommandLine;
using Data.Infrastructure;
using Spectre.Console;
using StackDuel.Infrastructure.Persistence.Migrations;

namespace Data.Commands;

internal static class SeedCommand
{
    public static Command Create()
    {
        Option<bool> dev = new("--dev") { Description = "Apply the local sample data." };
        Option<bool> e2e = new("--e2e") { Description = "Apply the fixtures the Playwright suites expect." };
        Option<bool> dryRun = new("--dry-run") { Description = "List the pending scripts without running them." };

        Command command = new("seed", "Apply throwaway data for a development or test database.");
        SharedOptions.AddTo(command);
        command.Options.Add(dev);
        command.Options.Add(e2e);
        command.Options.Add(dryRun);

        command.SetAction(parseResult =>
        {
            List<ScriptGroup> groups = [];

            if (parseResult.GetValue(dev))
                groups.Add(SeedScriptGroups.Dev);

            if (parseResult.GetValue(e2e))
                groups.Add(SeedScriptGroups.E2E);

            if (groups.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Pick what to seed:[/] --dev, --e2e, or both.");
                return UpgradeRunner.Failure;
            }

            DatabaseUpgrader upgrader = new(
                ConnectionStringResolver.Resolve(parseResult.GetValue(SharedOptions.Connection)),
                new SpectreUpgradeLog(parseResult.GetValue(SharedOptions.Verbose))
            );

            return UpgradeRunner.Run(upgrader, groups, parseResult.GetValue(dryRun));
        });

        return command;
    }
}
