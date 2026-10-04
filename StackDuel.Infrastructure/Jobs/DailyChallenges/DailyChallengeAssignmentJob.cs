using StackDuel.Application.Jobs.DailyChallenges;
using Microsoft.Extensions.Logging;
using Quartz;

namespace StackDuel.Infrastructure.Jobs.DailyChallenges;

[DisallowConcurrentExecution]
internal sealed partial class DailyChallengeAssignmentJob(
    IDailyChallengeAssignmentService assignmentService,
    ILogger<DailyChallengeAssignmentJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(DailyChallengeAssignmentJob), "DailyChallenges");

    public async Task Execute(IJobExecutionContext context)
    {
        LogExecuting();
        await assignmentService.AssignUpcomingChallengesAsync(context.CancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing DailyChallengeAssignmentJob")]
    private partial void LogExecuting();
}