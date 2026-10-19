using Microsoft.Extensions.Logging;
using Quartz;
using StackDuel.Application.Jobs.Users;

namespace StackDuel.Infrastructure.Jobs.Users;

[DisallowConcurrentExecution]
internal sealed partial class AvatarCleanupJob(IAvatarCleanupService cleanupService, ILogger<AvatarCleanupJob> logger)
    : IJob
{
    public static readonly JobKey Key = new(nameof(AvatarCleanupJob), "Users");

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        LogExecuting();
        await cleanupService.RunAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing AvatarCleanupJob")]
    private partial void LogExecuting();
}