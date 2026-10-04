using StackDuel.Application.Jobs.Submissions;
using Microsoft.Extensions.Logging;
using Quartz;

namespace StackDuel.Infrastructure.Jobs.Submissions;

[DisallowConcurrentExecution]
internal sealed partial class SubmissionCleanupJob(
    ISubmissionCleanupService cleanupService,
    ILogger<SubmissionCleanupJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(SubmissionCleanupJob), "Submissions");

    public async Task Execute(IJobExecutionContext context)
    {
        LogExecuting();
        await cleanupService.RunAsync(context.CancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing SubmissionCleanupJob")]
    private partial void LogExecuting();
}