using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StackDuel.Infrastructure.Persistence;

internal sealed class StackDuelDbContextFactory : IDesignTimeDbContextFactory<StackDuelDbContext>
{
    public StackDuelDbContext CreateDbContext(string[] args)
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        // Directory.GetCurrentDirectory() resolves to StackDuel.Api when run via
        // `dotnet ef --startup-project StackDuel.Api`, giving access to its appsettings.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No connection string found. Ensure 'ConnectionStrings:DefaultConnection' is configured."
            );

        var options = new DbContextOptionsBuilder<StackDuelDbContext>().UseNpgsql(connectionString).Options;

        return new StackDuelDbContext(options);
    }
}