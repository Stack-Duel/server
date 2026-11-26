using StackDuel.Data.Commands;
using System.CommandLine;

namespace StackDuel.Data;

internal static class DataCli
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand root = new("StackDuel database migration and seeding tool.")
        {
            MigrateCommand.Create(),
            SeedCommand.Create(),
            StatusCommand.Create(),
            BaselineCommand.Create(),
            ResetCommand.Create(),
            GrantAdminCommand.Create(),
        };

        return await root.Parse(args).InvokeAsync();
    }
}