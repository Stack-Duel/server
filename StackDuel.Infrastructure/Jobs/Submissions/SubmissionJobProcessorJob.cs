using Microsoft.Extensions.Logging;
using Quartz;

namespace StackDuel.Infrastructure.Jobs.Submissions;

[DisallowConcurrentExecution]
internal sealed partial class SubmissionJobProcessorJob(
    SubmissionJobProcessorService processorService,
    ILogger<SubmissionJobProcessorJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(SubmissionJobProcessorJob), "Submissions");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();

        await processorService.RunAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing SubmissionJobProcessorJob")]
    private partial void LogExecuting();
}