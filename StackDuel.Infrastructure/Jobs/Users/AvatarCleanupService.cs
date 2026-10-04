using StackDuel.Application.Jobs.Users;
using StackDuel.Application.Services.Users;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.Jobs.Users;

internal sealed partial class AvatarCleanupService(
    StackDuelDbContext context,
    IUserAvatarWriteRepository userAvatarRepository,
    IAvatarBlobStorage avatarBlobStorage,
    ILogger<AvatarCleanupService> logger
) : IAvatarCleanupService
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        LogRunning();

        List<Guid> userIds = await context.UserAvatars.Select(a => a.UserId).Distinct().ToListAsync(cancellationToken);

        int deletedCount = 0;

        foreach (Guid userId in userIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
                continue;

            IReadOnlyList<UserAvatar> avatars = await userAvatarRepository.GetAllByUserIdNewestFirstAsync(
                userId,
                cancellationToken
            );

            foreach (UserAvatar stale in AvatarRetentionPolicy.SelectStale(avatars, user.ImageUrl?.Value))
            {
                await avatarBlobStorage.DeleteAsync(stale.ImageUrl.Value, cancellationToken);
                await userAvatarRepository.DeleteAsync(stale, cancellationToken);
                deletedCount++;
            }
        }

        if (deletedCount > 0)
            LogDeleted(deletedCount);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Running avatar cleanup")]
    private partial void LogRunning();

    private void LogDeleted(int count) =>
        logger.LogInformation("Deleted {Count} unused avatar(s) during cleanup.", count);
}