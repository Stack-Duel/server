using StackDuel.Application.Pagination;
using StackDuel.Application.Users.Dtos;
using StackDuel.Application.Users.Dtos.Admin;

namespace StackDuel.Application.Users;

public interface IUserReadRepository
{
    Task<UserDto?> FindBySubAsync(string sub, CancellationToken cancellationToken);
    Task<UserDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, UserDto>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<UserProfileDto?> FindProfileByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<PageResult<AdminUserDto>> GetAdminUsersPageableAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    );
    Task<AdminUserDetailDto?> FindAdminUserDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
}