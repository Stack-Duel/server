using StackDuel.Application.Jobs.Submissions;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.Jobs.Submissions;

internal sealed partial class SubmissionCleanupService(
    StackDuelDbContext context,
    ILogger<SubmissionCleanupService> logger
) : ISubmissionCleanupService
{
    // Jobs stuck in Pending/Running longer than this are considered abandoned.
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(30);

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        LogRunning();

        var cutoff = DateTime.UtcNow - StaleThreshold;

        int count = await context
            .SubmissionJobs.Where(j =>
                (j.Status == SubmissionJobStatus.Pending || j.Status == SubmissionJobStatus.Running)
                && j.CreatedAt < cutoff
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(j => j.Status, SubmissionJobStatus.Failed)
                        .SetProperty(
                            j => j.FailureReason,
                            "Job abandoned: exceeded stale threshold without completing."
                        )
                        .SetProperty(j => j.CompletedAt, DateTime.UtcNow),
                cancellationToken
            );

        if (count > 0)
            LogAbandoned(count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Running submission cleanup")]
    private partial void LogRunning();

    private void LogAbandoned(int count) =>
        logger.LogWarning("Marked {Count} stale submission job(s) as failed.", count);
}