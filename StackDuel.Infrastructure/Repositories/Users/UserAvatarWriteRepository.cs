using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Users;

internal sealed class UserAvatarWriteRepository(StackDuelDbContext context) : IUserAvatarWriteRepository
{
    public async Task AddAsync(UserAvatar avatar, CancellationToken cancellationToken = default)
    {
        await context.UserAvatars.AddAsync(avatar, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserAvatar?> FindByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await context.UserAvatars.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserAvatar>> GetAllByUserIdNewestFirstAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .UserAvatars.Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserAvatar avatar, CancellationToken cancellationToken = default)
    {
        context.UserAvatars.Remove(avatar);
        await context.SaveChangesAsync(cancellationToken);
    }
}