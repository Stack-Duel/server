using System.CommandLine;

namespace Data.Commands;

internal static class SharedOptions
{
    public static Option<string?> Connection { get; } =
        new("--connection", "-c")
        {
            Description =
                "Postgres connection string. Falls back to ConnectionStrings__DefaultConnection, "
                + "then user secrets, then appsettings.json.",
        };

    public static Option<bool> Verbose { get; } =
        new("--verbose", "-v") { Description = "Echo every statement DbUp runs." };

    public static Option<string[]> Groups { get; } =
        new("--groups", "-g")
        {
            Description = "Limit the run to these script groups. Defaults to every group the command owns.",
            AllowMultipleArgumentsPerToken = true,
        };

    public static void AddTo(Command command)
    {
        command.Options.Add(Connection);
        command.Options.Add(Verbose);
    }
}
