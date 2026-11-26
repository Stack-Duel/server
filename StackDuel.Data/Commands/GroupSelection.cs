using Spectre.Console;
using StackDuel.Data.Migrations;

namespace StackDuel.Data.Commands;

internal static class GroupSelection
{
    public static bool TryResolve(
        string[] requestedNames,
        IReadOnlyList<ScriptGroup> allowed,
        out IReadOnlyList<ScriptGroup> selected
    )
    {
        if (requestedNames.Length == 0)
        {
            selected = allowed;
            return true;
        }

        List<ScriptGroup> resolved = [];

        foreach (string name in requestedNames)
        {
            ScriptGroup? match = allowed.FirstOrDefault(group =>
                group.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            );

            if (match is null)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Unknown group '{Markup.Escape(name)}'.[/] This command accepts: "
                        + string.Join(", ", allowed.Select(group => group.Name))
                );
                selected = [];
                return false;
            }

            resolved.Add(match);
        }

        selected = [.. allowed.Where(resolved.Contains)];
        return true;
    }
}