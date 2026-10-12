using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Jobs.Submissions;
using StackDuel.Application.Submissions;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Jobs.Submissions;

internal sealed partial class SubmissionCleanupService(
    StackDuelDbContext context,
    ISubmissionFailureService submissionFailureService,
    ILogger<SubmissionCleanupService> logger
) : ISubmissionCleanupService
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        LogRunning();

        DateTime cutoff = DateTime.UtcNow - SubmissionGradingWindow.StaleAfter;

        var staleJobs = await context
            .SubmissionJobs.Where(j =>
                (j.Status == SubmissionJobStatus.Pending || j.Status == SubmissionJobStatus.Running)
                && j.CreatedAt < cutoff
            )
            .Select(j => new { j.Id, j.SubmissionId })
            .ToListAsync(cancellationToken);

        if (staleJobs.Count == 0)
            return;

        Guid[] staleJobIds = [.. staleJobs.Select(j => j.Id)];

        int count = await context
            .SubmissionJobs.Where(j => staleJobIds.Contains(j.Id))
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

        foreach (Guid submissionId in staleJobs.Select(j => j.SubmissionId).Distinct())
            await submissionFailureService.MarkFailedAsync(submissionId, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Running submission cleanup")]
    private partial void LogRunning();

    private void LogAbandoned(int count) =>
        logger.LogWarning("Marked {Count} stale submission job(s) as failed.", count);
}
