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

    public async Task Execute(IJobExecutionContext context)
    {
        LogExecuting();
        await processorService.RunAsync(context.CancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing TestCaseGenerationJobProcessorJob")]
    private partial void LogExecuting();
}