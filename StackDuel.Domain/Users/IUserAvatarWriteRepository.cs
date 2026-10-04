using StackDuel.Domain.Users.Entities;

namespace StackDuel.Domain.Users;

public interface IUserAvatarWriteRepository
{
    Task AddAsync(UserAvatar avatar, CancellationToken cancellationToken = default);
    Task<UserAvatar?> FindByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAvatar>> GetAllByUserIdNewestFirstAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(UserAvatar avatar, CancellationToken cancellationToken = default);
}