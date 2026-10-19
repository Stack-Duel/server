using Microsoft.Extensions.Logging;
using Quartz;
using StackDuel.Application.TestCaseGeneration;

namespace StackDuel.Infrastructure.Jobs.TestCaseGeneration;

[DisallowConcurrentExecution]
internal sealed partial class TestCaseRefreshJob(ITestCaseRefreshService refreshService, ILogger<TestCaseRefreshJob> logger)
    : IJob
{
    public static readonly JobKey Key = new(nameof(TestCaseRefreshJob), "TestCaseGeneration");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();
        int enqueued = await refreshService.EnqueueRefreshesAsync(cancellationToken);
        LogCompleted(enqueued);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing TestCaseRefreshJob")]
    private partial void LogExecuting();

    [LoggerMessage(Level = LogLevel.Information, Message = "TestCaseRefreshJob enqueued {Count} refresh job(s)")]
    private partial void LogCompleted(int count);
}