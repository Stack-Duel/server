using StackDuel.Application.Jobs.Users;
using Microsoft.Extensions.Logging;
using Quartz;

namespace StackDuel.Infrastructure.Jobs.Users;

[DisallowConcurrentExecution]
internal sealed partial class AvatarCleanupJob(IAvatarCleanupService cleanupService, ILogger<AvatarCleanupJob> logger)
    : IJob
{
    public static readonly JobKey Key = new(nameof(AvatarCleanupJob), "Users");

    public async Task Execute(IJobExecutionContext context)
    {
        LogExecuting();
        await cleanupService.RunAsync(context.CancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing AvatarCleanupJob")]
    private partial void LogExecuting();
}