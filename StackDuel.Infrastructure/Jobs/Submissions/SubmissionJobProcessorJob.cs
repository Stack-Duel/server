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

    public async Task Execute(IJobExecutionContext context)
    {
        LogExecuting();

        await processorService.RunAsync(context.CancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing SubmissionJobProcessorJob")]
    private partial void LogExecuting();
}