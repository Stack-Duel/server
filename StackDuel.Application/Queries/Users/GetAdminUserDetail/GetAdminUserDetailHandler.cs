using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos.Admin;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Users.GetAdminUserDetail;

internal sealed class GetAdminUserDetailHandler(IUserReadRepository userReadRepository)
    : IQueryHandler<GetAdminUserDetailQuery, AdminUserDetailDto>
{
    public async Task<Result<AdminUserDetailDto>> Handle(
        GetAdminUserDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        var user = await userReadRepository.FindAdminUserDetailByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<AdminUserDetailDto>.NotFound();

        return Result.Success(user);
    }
}