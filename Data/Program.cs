using System.CommandLine;
using Data.Commands;

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
