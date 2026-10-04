using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Audit;
using StackDuel.Domain.Audit.Entities;
using StackDuel.Domain.Authorization.Rbac;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.ValueObjects;
using StackDuel.Infrastructure;
using StackDuel.Infrastructure.Persistence.Seeders;

bool seedStatic = args.Contains("--static") || args.Contains("--all");
bool seedDemo = args.Contains("--demo") || args.Contains("--all");
string? grantAdminUsername = args.SkipWhile(a => a != "--grant-admin").Skip(1).FirstOrDefault();

if (!seedStatic && !seedDemo && grantAdminUsername is null)
{
    Console.WriteLine(
        "Usage: dotnet run --project StackDuel.Seeder -- [--static] [--demo] [--all] [--grant-admin <username>]"
    );
    Console.WriteLine();
    Console.WriteLine(
        "  --static              Seed reference data (languages, versions, problems) — idempotent, safe every deploy"
    );
    Console.WriteLine("  --demo                Seed demo data (example problems and test suites)");
    Console.WriteLine("  --all                 Seed everything");
    Console.WriteLine("  --grant-admin <name>  Add an existing user to the admin group, by username");
    return 1;
}

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

ServiceCollection services = new();
services.AddInfrastructureForSeeder(configuration);

await using ServiceProvider provider = services.BuildServiceProvider();
IServiceProvider sp = provider;

await sp.MigrateAsync();

if (seedStatic || seedDemo)
{
    await sp.SeedAsync(new SeederOptions { SeedStaticData = seedStatic, SeedDemoData = seedDemo });
}

if (grantAdminUsername is not null)
{
    using IServiceScope scope = sp.CreateScope();
    IUserWriteRepository userWriteRepository = scope.ServiceProvider.GetRequiredService<IUserWriteRepository>();

    var user = await userWriteRepository.FindByUsername(new Username(grantAdminUsername));
    if (user is null)
    {
        Console.WriteLine($"No user found with username '{grantAdminUsername}'.");
        return 1;
    }

    await userWriteRepository.AddToGroupAsync(user.Id, WellKnownAuthorization.AdminGroup);

    IAuditLogWriteRepository auditLogWriteRepository =
        scope.ServiceProvider.GetRequiredService<IAuditLogWriteRepository>();
    await auditLogWriteRepository.AddAsync(
        AuditLogEntry.Create(
            actorUserId: null,
            actorUsername: "cli:seeder",
            action: "user.groups.admin-granted",
            targetType: "user",
            targetId: user.Id.ToString(),
            detailsJson: null
        )
    );

    Console.WriteLine($"Granted the admin group to '{grantAdminUsername}'.");
}

Console.WriteLine("Done.");
return 0;