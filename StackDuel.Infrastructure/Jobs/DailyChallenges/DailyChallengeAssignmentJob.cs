using Microsoft.Extensions.Logging;
using Quartz;
using StackDuel.Application.Jobs.DailyChallenges;

namespace StackDuel.Infrastructure.Jobs.DailyChallenges;

[DisallowConcurrentExecution]
internal sealed partial class DailyChallengeAssignmentJob(
    IDailyChallengeAssignmentService assignmentService,
    ILogger<DailyChallengeAssignmentJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(DailyChallengeAssignmentJob), "DailyChallenges");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();
        await assignmentService.AssignUpcomingChallengesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing DailyChallengeAssignmentJob")]
    private partial void LogExecuting();
}