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

    public async Task Execute(IJobExecutionContext context)
    {
        LogExecuting();
        await sweepService.RunAsync(context.CancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing GameExpirySweepJob")]
    private partial void LogExecuting();
}