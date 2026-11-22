using System.CommandLine;
using Data.Infrastructure;
using Spectre.Console;
using StackDuel.Infrastructure.Persistence.Migrations;

namespace Data.Commands;

internal static class StatusCommand
{
    public static Command Create()
    {
        Command command = new("status", "Show what each script group has applied and what is still pending.");
        SharedOptions.AddTo(command);
        command.Options.Add(SharedOptions.Groups);

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

            Write([.. groups.Select(upgrader.GetStatus)]);

            return UpgradeRunner.Success;
        });

        return command;
    }

    private static void Write(IReadOnlyList<GroupStatus> statuses)
    {
        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Group")
            .AddColumn("Applied", column => column.RightAligned())
            .AddColumn("Pending", column => column.RightAligned())
            .AddColumn("Next up");

        foreach (GroupStatus status in statuses)
        {
            table.AddRow(
                status.Group.Name,
                status.AppliedScripts.Count.ToString(),
                status.IsUpToDate ? "[green]0[/]" : $"[yellow]{status.PendingScripts.Count}[/]",
                status.IsUpToDate ? "[grey]up to date[/]" : Markup.Escape(status.PendingScripts[0])
            );
        }

        AnsiConsole.Write(table);

        foreach (GroupStatus status in statuses.Where(status => status.OrphanedScripts.Count > 0))
        {
            AnsiConsole.MarkupLine(
                $"[yellow]{status.Group.Name}[/] journals scripts missing from this build: "
                    + Markup.Escape(string.Join(", ", status.OrphanedScripts))
            );
        }
    }
}
