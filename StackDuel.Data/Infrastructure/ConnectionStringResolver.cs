using Microsoft.Extensions.Configuration;

namespace StackDuel.Data.Infrastructure;

internal static class ConnectionStringResolver
{
    public static string Resolve(string? fromCommandLine)
    {
        if (!string.IsNullOrWhiteSpace(fromCommandLine))
            return fromCommandLine;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ConnectionStringResolver).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? configured = configuration.GetConnectionString("DefaultConnection");

        return string.IsNullOrWhiteSpace(configured)
            ? throw new InvalidOperationException(
                "No connection string. Pass --connection, set ConnectionStrings__DefaultConnection, "
                    + "or run: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<connection>\"."
            )
            : configured;
    }
}