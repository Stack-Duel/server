using Ardalis.Result;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.User;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Application.Queries.Users.GetUserBySub;

internal sealed class GetUserBySubHandler(IUserRepository userRepository) : IQueryHandler<GetUserBySubQuery, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(GetUserBySubQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindBySubAsync(new UserSub(query.Sub), cancellationToken);

        if (user is null)
            return Result.NotFound();

        return Result.Success(
            new UserDto(user.Id, user.Username.Value, user.Sub.Value, user.ImageUrl?.Value, user.Bio?.Value, user.Tenant)
        );
    }
}