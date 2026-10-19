using Microsoft.Extensions.Logging;
using Quartz;
using StackDuel.Application.Jobs.Games;

namespace StackDuel.Infrastructure.Jobs.Games;

[DisallowConcurrentExecution]
internal sealed partial class GameExpirySweepJob(
    IGameExpirySweepService sweepService,
    ILogger<GameExpirySweepJob> logger
) : IJob
{
    public static readonly JobKey Key = new(nameof(GameExpirySweepJob), "Games");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();
        await sweepService.RunAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing GameExpirySweepJob")]
    private partial void LogExecuting();
}