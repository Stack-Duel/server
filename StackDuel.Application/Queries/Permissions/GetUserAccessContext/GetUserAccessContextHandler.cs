using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Authorization;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Permissions.GetUserAccessContext;

internal sealed class GetUserAccessContextHandler(
    IUserReadRepository userReadRepository,
    IAuthorizationReadRepository authorizationReadRepository
) : IQueryHandler<GetUserAccessContextQuery, UserAccessContextDto>
{
    public async Task<Result<UserAccessContextDto>> Handle(
        GetUserAccessContextQuery request,
        CancellationToken cancellationToken
    )
    {
        UserDto? user = await userReadRepository.FindBySubAsync(request.Sub, cancellationToken);

        if (user is null)
            return Result.NotFound();

        IReadOnlyList<string> permissions = await authorizationReadRepository.GetUserPermissionsAsync(
            user.Id,
            cancellationToken
        );

        IReadOnlyList<string> roles = await authorizationReadRepository.GetUserRoleNamesAsync(
            user.Id,
            cancellationToken
        );

        return Result.Success(new UserAccessContextDto(user, permissions, roles));
    }
}