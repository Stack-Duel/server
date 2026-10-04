using Ardalis.Result;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Queries.Users.GetUserBySub;

internal sealed class GetUserBySubHandler(IUserReadRepository userReadRepository)
    : IQueryHandler<GetUserBySubQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(GetUserBySubQuery request, CancellationToken cancellationToken)
    {
        var user = await userReadRepository.FindBySubAsync(request.Sub, cancellationToken);

        if (user is null)
            return Result.NotFound();

        return Result.Success(user);
    }
}