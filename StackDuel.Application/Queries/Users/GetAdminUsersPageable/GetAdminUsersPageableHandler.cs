using Ardalis.Result;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos.Admin;

namespace StackDuel.Application.Queries.Users.GetAdminUsersPageable;

internal sealed class GetAdminUsersPageableHandler(IUserReadRepository userReadRepository)
    : IQueryHandler<GetAdminUsersPageableQuery, PageResult<AdminUserDto>>
{
    public async Task<Result<PageResult<AdminUserDto>>> Handle(
        GetAdminUsersPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await userReadRepository.GetAdminUsersPageableAsync(
            request.PaginationRequest,
            request.Search,
            cancellationToken
        );

        return Result.Success(result);
    }
}