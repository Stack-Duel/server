using Microsoft.Extensions.Logging;
using Quartz;
using StackDuel.Application.Jobs.Submissions;

namespace StackDuel.Infrastructure.Jobs.Submissions;

[DisallowConcurrentExecution]
internal sealed partial class SubmissionCleanupJob(
    ISubmissionCleanupService cleanupService,
    ILogger<SubmissionCleanupJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(SubmissionCleanupJob), "Submissions");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();
        await cleanupService.RunAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing SubmissionCleanupJob")]
    private partial void LogExecuting();
}