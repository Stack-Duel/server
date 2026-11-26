using Spectre.Console;
using StackDuel.Data.Seeding;

namespace StackDuel.Data.Commands;

internal static class ChinookBundleStep
{
    public static void Ensure(string connectionString)
    {
        bool inserted = FileBundleSeeder.EnsureChinookBundle(connectionString);

        AnsiConsole.MarkupLine(
            inserted
                ? "[green]file bundles: inserted the Chinook SQLite database.[/]"
                : "[grey]file bundles: Chinook already present.[/]"
        );
    }
}