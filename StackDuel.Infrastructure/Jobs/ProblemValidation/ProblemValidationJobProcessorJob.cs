using Microsoft.Extensions.Logging;
using Quartz;

namespace StackDuel.Infrastructure.Jobs.ProblemValidation;

[DisallowConcurrentExecution]
internal sealed partial class ProblemValidationJobProcessorJob(
    ProblemValidationJobProcessorService processorService,
    ILogger<ProblemValidationJobProcessorJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(ProblemValidationJobProcessorJob), "ProblemValidation");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();
        await processorService.RunAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing ProblemValidationJobProcessorJob")]
    private partial void LogExecuting();
}