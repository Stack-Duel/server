using StackDuel.Application.Configuration;
using StackDuel.Application.LanguageServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.LanguageServer;

internal sealed class LanguageServerIdleSessionReaper(
    ILanguageServerSessionManager sessionManager,
    LanguageServerOptions options,
    ILogger<LanguageServerIdleSessionReaper> logger
) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
            return;

        try
        {
            await sessionManager.SweepOrphanedWorkspacesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sweep orphaned language server workspaces on startup.");
        }

        TimeSpan idleTimeout = TimeSpan.FromMinutes(Math.Max(1, options.IdleTimeoutMinutes));
        using PeriodicTimer timer = new(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await sessionManager.ReapIdleSessionsAsync(idleTimeout, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to reap idle language server sessions.");
            }
        }
    }
}