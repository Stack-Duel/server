namespace StackDuel.Domain.Authorization;

public interface IAuthorizationReadRepository
{
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
}