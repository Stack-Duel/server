using Microsoft.Extensions.Logging;
using Quartz;

namespace StackDuel.Infrastructure.Jobs.TestCaseGeneration;

[DisallowConcurrentExecution]
internal sealed partial class TestCaseGenerationJobProcessorJob(
    TestCaseGenerationJobProcessorService processorService,
    ILogger<TestCaseGenerationJobProcessorJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(TestCaseGenerationJobProcessorJob), "TestCaseGeneration");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();
        await processorService.RunAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing TestCaseGenerationJobProcessorJob")]
    private partial void LogExecuting();
}