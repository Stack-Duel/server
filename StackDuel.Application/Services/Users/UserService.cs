using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using StackDuel.Application.Commands.Users.SelectUserAvatar;
using StackDuel.Application.Commands.Users.UpdateProfilePrivacy;
using StackDuel.Application.Commands.Users.UpdateUserGroups;
using StackDuel.Application.Commands.Users.UploadUserAvatar;
using StackDuel.Application.Commands.Users.UpsertUser;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries.Permissions.GetUserAccessContext;
using StackDuel.Application.Queries.Users.GetAdminUserDetail;
using StackDuel.Application.Queries.Users.GetAdminUsersPageable;
using StackDuel.Application.Queries.Users.GetUserAvatarHistory;
using StackDuel.Application.Queries.Users.GetUserBySub;
using StackDuel.Application.Queries.Users.GetUserGameStats;
using StackDuel.Application.Queries.Users.GetUserProfileByUsername;
using StackDuel.Application.Users.Dtos;
using StackDuel.Application.Users.Dtos.Admin;

namespace StackDuel.Application.Services.Users;

public interface IUserService
{
    Task<Result<UserDto>> GetBySubAsync(string sub, CancellationToken cancellationToken);

    Task<Result<UserAccessContextDto>> GetAccessContextBySubAsync(string sub, CancellationToken cancellationToken);

    void InvalidateAccessContext(string sub);

    Task<Result<UserProfileDto>> GetProfileByUsernameAsync(
        string username,
        Guid? requestingUserId,
        CancellationToken cancellationToken
    );

    Task<Result<UserGameStatsDto>> GetGameStatsByUsernameAsync(
        string username,
        Guid? requestingUserId,
        CancellationToken cancellationToken
    );

    Task<Result> UpsertAccountAsync(string sub, UpsertUserDto request, CancellationToken cancellationToken);

    Task<Result<PageResult<AdminUserDto>>> GetAdminUsersPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    );

    Task<Result<AdminUserDetailDto>> GetAdminUserDetailAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result> UpdateUserGroupsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken
    );

    Task<Result> UpdateProfilePrivacyAsync(Guid userId, bool isPrivate, CancellationToken cancellationToken);

    Task<Result<string>> UploadAvatarAsync(Guid userId, byte[] content, CancellationToken cancellationToken);

    Task<Result> SelectAvatarAsync(Guid userId, Guid avatarId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<UserAvatarDto>>> GetAvatarHistoryAsync(Guid userId, CancellationToken cancellationToken);
}

internal sealed class UserService(IMediator mediator, IMemoryCache cache) : IUserService
{
    private static readonly TimeSpan AccessContextCacheDuration = TimeSpan.FromSeconds(60);

    private static string AccessContextCacheKey(string sub) => $"user-access-context:{sub}";

    public async Task<Result<UserAccessContextDto>> GetAccessContextBySubAsync(
        string sub,
        CancellationToken cancellationToken
    )
    {
        string cacheKey = AccessContextCacheKey(sub);

        if (cache.TryGetValue(cacheKey, out Result<UserAccessContextDto>? cached) && cached is not null)
            return cached;

        var result = await mediator.Send(new GetUserAccessContextQuery(sub), cancellationToken);

        if (result.IsSuccess)
            cache.Set(cacheKey, result, AccessContextCacheDuration);

        return result;
    }

    public void InvalidateAccessContext(string sub) => cache.Remove(AccessContextCacheKey(sub));

    public async Task<Result<UserDto>> GetBySubAsync(string sub, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserBySubQuery(sub), cancellationToken);
        return result;
    }

    public async Task<Result<UserProfileDto>> GetProfileByUsernameAsync(
        string username,
        Guid? requestingUserId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new GetUserProfileByUsernameQuery(username, requestingUserId),
            cancellationToken
        );
        return result;
    }

    public async Task<Result<UserGameStatsDto>> GetGameStatsByUsernameAsync(
        string username,
        Guid? requestingUserId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetUserGameStatsQuery(username, requestingUserId), cancellationToken);
        return result;
    }

    public async Task<Result> UpsertAccountAsync(string sub, UpsertUserDto request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpsertUserCommand(
                sub,
                request.Username,
                request.ImageUrl,
                request.Bio,
                request.LanguageIds,
                request.TenantId
            ),
            cancellationToken
        );
        return result;
    }

    public async Task<Result<PageResult<AdminUserDto>>> GetAdminUsersPageableAsync(
        PaginationRequest paginationRequest,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetAdminUsersPageableQuery(paginationRequest, search), cancellationToken);
        return result;
    }

    public async Task<Result<AdminUserDetailDto>> GetAdminUserDetailAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetAdminUserDetailQuery(userId), cancellationToken);
        return result;
    }

    public async Task<Result> UpdateUserGroupsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new UpdateUserGroupsCommand(userId, groupIds), cancellationToken);
        return result;
    }

    public async Task<Result> UpdateProfilePrivacyAsync(
        Guid userId,
        bool isPrivate,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new UpdateProfilePrivacyCommand(userId, isPrivate), cancellationToken);
        return result;
    }

    public async Task<Result<string>> UploadAvatarAsync(
        Guid userId,
        byte[] content,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new UploadUserAvatarCommand(userId, content), cancellationToken);
        return result;
    }

    public async Task<Result> SelectAvatarAsync(Guid userId, Guid avatarId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SelectUserAvatarCommand(userId, avatarId), cancellationToken);
        return result;
    }

    public async Task<Result<IReadOnlyList<UserAvatarDto>>> GetAvatarHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new GetUserAvatarHistoryQuery(userId), cancellationToken);
        return result;
    }
}