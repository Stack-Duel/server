using Data.Migrations;
using Spectre.Console;

namespace Data.Infrastructure;

internal static class UpgradeRunner
{
    public const int Success = 0;
    public const int Failure = 1;

    public static int Run(DatabaseUpgrader upgrader, IReadOnlyList<ScriptGroup> groups, bool dryRun)
    {
        if (!upgrader.TryConnect(out string connectionError))
        {
            AnsiConsole.MarkupLine($"[red]Cannot reach the database:[/] {Markup.Escape(connectionError)}");
            return Failure;
        }

        List<GroupStatus> statuses = [.. groups.Select(upgrader.GetStatus)];
        int pendingCount = statuses.Sum(status => status.ScriptsToRun);

        WarnAboutOrphans(statuses);

        if (pendingCount == 0)
        {
            AnsiConsole.MarkupLine("[green]Already up to date.[/] Nothing to apply.");
            return Success;
        }

        if (dryRun)
        {
            ReportPending(statuses);
            return Success;
        }

        return Apply(upgrader, statuses, pendingCount);
    }

    private static int Apply(DatabaseUpgrader upgrader, List<GroupStatus> statuses, int pendingCount)
    {
        GroupUpgradeResult? failure = null;

        AnsiConsole
            .Progress()
            .Columns(
                new TaskDescriptionColumn { Alignment = Justify.Left },
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn()
            )
            .Start(context =>
            {
                foreach (GroupStatus status in statuses.Where(status => !status.IsUpToDate))
                {
                    ProgressTask task = context.AddTask(
                        $"[bold]{status.Group.Name}[/] ({status.ScriptsToRun})",
                        maxValue: status.ScriptsToRun
                    );

                    GroupUpgradeResult result = upgrader.Upgrade(status.Group, scriptName =>
                    {
                        task.Description = $"[bold]{status.Group.Name}[/] {Markup.Escape(scriptName)}";
                        task.Increment(1);
                    });

                    if (result.Succeeded)
                    {
                        task.Description = $"[bold green]{status.Group.Name}[/] ({result.ExecutedScripts.Count})";
                        task.Value = task.MaxValue;
                        continue;
                    }

                    task.Description = $"[bold red]{status.Group.Name}[/] failed";
                    task.StopTask();
                    failure = result;
                    break;
                }
            });

        if (failure is null)
        {
            AnsiConsole.MarkupLine($"[green]Applied {pendingCount} script(s).[/]");
            return Success;
        }

        AnsiConsole.MarkupLine(
            $"[red]The '{failure.Group.Name}' group failed after {failure.ExecutedScripts.Count} script(s).[/]"
        );
        AnsiConsole.MarkupLine("[grey]Scripts that already succeeded stay journaled. Fix the SQL and re-run.[/]");

        if (failure.Error is not null)
            AnsiConsole.WriteException(failure.Error, ExceptionFormats.ShortenPaths);

        return Failure;
    }

    private static void ReportPending(List<GroupStatus> statuses)
    {
        Table table = new Table().Border(TableBorder.Rounded).AddColumn("Group").AddColumn("Script").AddColumn("Kind");

        foreach (GroupStatus status in statuses.Where(status => !status.IsUpToDate))
        {
            foreach (string script in status.PendingScripts)
                table.AddRow(Markup.Escape(status.Group.Name), Markup.Escape(script), "pending");

            foreach (string script in status.RepeatableScripts)
                table.AddRow(Markup.Escape(status.Group.Name), Markup.Escape(script), "[blue]repeatable[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[yellow]Dry run.[/] Nothing was applied.");
    }

    private static void WarnAboutOrphans(List<GroupStatus> statuses)
    {
        foreach (GroupStatus status in statuses.Where(status => status.OrphanedScripts.Count > 0))
        {
            AnsiConsole.MarkupLine(
                $"[yellow]The '{status.Group.Name}' journal records {status.OrphanedScripts.Count} script(s) "
                    + "that no longer exist in this build:[/] "
                    + Markup.Escape(string.Join(", ", status.OrphanedScripts))
            );
        }
    }
}