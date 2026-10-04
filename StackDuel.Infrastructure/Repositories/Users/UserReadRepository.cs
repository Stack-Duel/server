using StackDuel.Application.Groups.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Application.Users.Dtos.Admin;
using StackDuel.Infrastructure.Persistence.Read;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Users;

internal sealed class UserReadRepository(StackDuelReadDbContext context) : IUserReadRepository
{
    public async Task<UserDto?> FindBySubAsync(string sub, CancellationToken cancellationToken)
    {
        var user = await context
            .Users.AsNoTracking()
            .Where(u => u.Sub == sub)
            .Select(u => new UserDto(
                u.Id,
                u.Sub,
                u.Username,
                u.ImageUrl,
                u.Bio,
                u.IsPrivate,
                UsernameLastChangedAt: u.UsernameLastChangedAt,
                CreatedAt: u.CreatedAt,
                SetupCompletedAt: u.SetupCompletedAt,
                LanguagePreferenceIds: Array.Empty<Guid>()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        IReadOnlyList<Guid> languagePreferenceIds = await GetLanguagePreferenceIdsAsync(user.Id, cancellationToken);

        return user with
        {
            LanguagePreferenceIds = languagePreferenceIds,
        };
    }

    public async Task<UserDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context
            .Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto(
                u.Id,
                u.Sub,
                u.Username,
                u.ImageUrl,
                u.Bio,
                u.IsPrivate,
                UsernameLastChangedAt: u.UsernameLastChangedAt,
                CreatedAt: u.CreatedAt,
                SetupCompletedAt: u.SetupCompletedAt,
                LanguagePreferenceIds: Array.Empty<Guid>()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, UserDto>> FindByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken
    )
    {
        Guid[] idArray = [.. ids];
        return await context
            .Users.AsNoTracking()
            .Where(u => idArray.Contains(u.Id))
            .Select(u => new UserDto(
                u.Id,
                u.Sub,
                u.Username,
                u.ImageUrl,
                u.Bio,
                u.IsPrivate,
                UsernameLastChangedAt: u.UsernameLastChangedAt,
                CreatedAt: u.CreatedAt,
                SetupCompletedAt: u.SetupCompletedAt,
                LanguagePreferenceIds: Array.Empty<Guid>()
            ))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> GetLanguagePreferenceIdsAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await context
            .UserLanguagePreferences.Where(ulp => ulp.UserId == userId)
            .OrderBy(ulp => ulp.Position)
            .Select(ulp => ulp.LanguageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProfileDto?> FindProfileByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await context
            .Users.AsNoTracking()
            .Where(u => u.Username == username)
            .Select(u => new UserProfileDto(
                u.Id,
                u.Username,
                u.Bio,
                u.ImageUrl,
                u.CreatedAt,
                u.IsPrivate,
                false,
                null,
                null,
                null
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PageResult<AdminUserDto>> GetAdminUsersPageableAsync(
        PaginationRequest pagination,
        string? search,
        CancellationToken cancellationToken = default
    )
    {
        int offset = (pagination.Page - 1) * pagination.Size;

        var query = context.Users.AsNoTracking().AsQueryable();

        string? term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            if (Guid.TryParse(term, out Guid id))
            {
                query = query.Where(u => u.Id == id);
            }
            else
            {
                string usernameTerm = term.ToLowerInvariant();
                query = query.Where(u => u.Username.ToLower().Contains(usernameTerm));
            }
        }

        int total = await query.CountAsync(cancellationToken);

        var pageUsers = await query
            .OrderBy(u => u.Id)
            .Skip(offset)
            .Take(pagination.Size)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.ImageUrl,
                u.UsernameLastChangedAt,
                u.CreatedAt,
                u.SetupCompletedAt,
            })
            .ToListAsync(cancellationToken);

        Guid[] userIds = [.. pageUsers.Select(u => u.Id)];

        var groupRows = await (
            from userGroup in context.UserGroups
            join grp in context.Groups on userGroup.GroupId equals grp.Id
            where userIds.Contains(userGroup.UserId)
            select new
            {
                userGroup.UserId,
                GroupId = grp.Id,
                GroupName = grp.Name,
            }
        ).ToListAsync(cancellationToken);

        Dictionary<Guid, List<GroupDto>> groupsByUser = groupRows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => new GroupDto(r.GroupId, r.GroupName)).ToList());

        return new PageResult<AdminUserDto>
        {
            Results =
            [
                .. pageUsers.Select(u => new AdminUserDto(
                    u.Id,
                    u.Username,
                    u.ImageUrl,
                    u.UsernameLastChangedAt,
                    u.CreatedAt,
                    u.SetupCompletedAt,
                    groupsByUser.TryGetValue(u.Id, out var groups) ? groups : []
                )),
            ],
            Total = total,
            Page = pagination.Page,
            Size = pagination.Size,
        };
    }

    public async Task<AdminUserDetailDto?> FindAdminUserDetailByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var user = await context
            .Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.ImageUrl,
                u.Bio,
                u.IsPrivate,
                u.UsernameLastChangedAt,
                u.CreatedAt,
                u.SetupCompletedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var groups = await (
            from userGroup in context.UserGroups
            join grp in context.Groups on userGroup.GroupId equals grp.Id
            where userGroup.UserId == id
            select new GroupDto(grp.Id, grp.Name)
        ).ToListAsync(cancellationToken);

        return new AdminUserDetailDto(
            user.Id,
            user.Username,
            user.ImageUrl,
            user.Bio,
            user.IsPrivate,
            user.UsernameLastChangedAt,
            user.CreatedAt,
            user.SetupCompletedAt,
            groups
        );
    }
}