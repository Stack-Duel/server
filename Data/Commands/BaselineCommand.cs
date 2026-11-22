using System.CommandLine;
using Data.Infrastructure;
using Spectre.Console;
using StackDuel.Infrastructure.Persistence.Migrations;

namespace Data.Commands;

internal static class BaselineCommand
{
    public static Command Create()
    {
        Option<bool> yes = new("--yes") { Description = "Skip the confirmation prompt." };

        Command command = new(
            "baseline",
            "Journal the pending scripts as already applied, without running them. "
                + "Run this once against a database that predates DbUp."
        );
        SharedOptions.AddTo(command);
        command.Options.Add(SharedOptions.Groups);
        command.Options.Add(yes);

        command.SetAction(parseResult =>
        {
            if (
                !GroupSelection.TryResolve(
                    parseResult.GetValue(SharedOptions.Groups) ?? [],
                    SeedScriptGroups.Everything,
                    out IReadOnlyList<ScriptGroup> groups
                )
            )
                return UpgradeRunner.Failure;

            DatabaseUpgrader upgrader = new(
                ConnectionStringResolver.Resolve(parseResult.GetValue(SharedOptions.Connection)),
                new SpectreUpgradeLog(parseResult.GetValue(SharedOptions.Verbose))
            );

            if (!upgrader.TryConnect(out string connectionError))
            {
                AnsiConsole.MarkupLine($"[red]Cannot reach the database:[/] {Markup.Escape(connectionError)}");
                return UpgradeRunner.Failure;
            }

            if (!parseResult.GetValue(yes) && !Confirm(groups))
                return UpgradeRunner.Failure;

            foreach (ScriptGroup group in groups)
            {
                IReadOnlyList<string> marked = upgrader.MarkAsExecuted(group);

                AnsiConsole.MarkupLine(
                    marked.Count == 0
                        ? $"[grey]{group.Name}: nothing pending.[/]"
                        : $"[green]{group.Name}: journaled {marked.Count} script(s) as applied.[/]"
                );
            }

            return UpgradeRunner.Success;
        });

        return command;
    }

    private static bool Confirm(IReadOnlyList<ScriptGroup> groups)
    {
        AnsiConsole.MarkupLine(
            "[yellow]Baselining marks every pending script in "
                + $"{Markup.Escape(string.Join(", ", groups.Select(group => group.Name)))} as applied "
                + "without executing it.[/] Only do this when the database already has that state."
        );

        return AnsiConsole.Confirm("Continue?", defaultValue: false);
    }
}
