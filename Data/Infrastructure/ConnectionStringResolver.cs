using Microsoft.Extensions.Configuration;

namespace Data.Infrastructure;

internal static class ConnectionStringResolver
{
    public static string Resolve(string? fromCommandLine)
    {
        if (!string.IsNullOrWhiteSpace(fromCommandLine))
            return fromCommandLine;

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets(typeof(ConnectionStringResolver).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? configured = configuration.GetConnectionString("DefaultConnection");

        return string.IsNullOrWhiteSpace(configured)
            ? throw new InvalidOperationException(
                "No connection string. Pass --connection, set ConnectionStrings__DefaultConnection, "
                    + "or add ConnectionStrings:DefaultConnection to user secrets or appsettings.json."
            )
            : configured;
    }
}
