using DbUp.Engine.Output;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Settings;

namespace StackDuel.Infrastructure.Persistence.Migrations;

public static class DatabaseUpgradeServiceExtensions
{
    public static void UpgradeDatabase(this IServiceProvider services, params ScriptGroup[] groups)
    {
        using IServiceScope scope = services.CreateScope();

        var connectionStrings = scope.ServiceProvider.GetRequiredService<ConnectionStringOptions>();
        ILogger logger = scope
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseUpgrader).FullName!);

        IUpgradeLog log = new MicrosoftUpgradeLog(logger);
        DatabaseUpgrader upgrader = new(connectionStrings.DefaultConnection, log);

        foreach (ScriptGroup group in groups.Length > 0 ? groups : ScriptGroups.All)
        {
            GroupUpgradeResult result = upgrader.Upgrade(group);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Applying the '{group.Name}' scripts failed. The journal is unchanged for the failing script.",
                    result.Error
                );

            if (result.ExecutedScripts.Count > 0)
                logger.LogInformation(
                    "Applied {ScriptCount} {Group} script(s): {Scripts}",
                    result.ExecutedScripts.Count,
                    group.Name,
                    string.Join(", ", result.ExecutedScripts)
                );
        }
    }
}