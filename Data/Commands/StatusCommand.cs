using Data.Infrastructure;
using Data.Migrations;
using Spectre.Console;
using System.CommandLine;

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

    private static string NextUp(GroupStatus status)
    {
        if (status.PendingScripts.Count > 0)
            return Markup.Escape(status.PendingScripts[0]);

        return status.RepeatableScripts.Count > 0 ? "[grey]repeatable only[/]" : "[grey]up to date[/]";
    }

    private static void Write(IReadOnlyList<GroupStatus> statuses)
    {
        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Group")
            .AddColumn("Applied", column => column.RightAligned())
            .AddColumn("Pending", column => column.RightAligned())
            .AddColumn("Repeatable", column => column.RightAligned())
            .AddColumn("Next up");

        foreach (GroupStatus status in statuses)
        {
            table.AddRow(
                status.Group.Name,
                status.AppliedScripts.Count.ToString(),
                status.PendingScripts.Count == 0 ? "[green]0[/]" : $"[yellow]{status.PendingScripts.Count}[/]",
                status.RepeatableScripts.Count == 0 ? "[grey]0[/]" : $"[blue]{status.RepeatableScripts.Count}[/]",
                NextUp(status)
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