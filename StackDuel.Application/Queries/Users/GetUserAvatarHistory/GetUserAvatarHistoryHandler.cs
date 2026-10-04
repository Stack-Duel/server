using Ardalis.Result;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Users;
using StackDuel.Domain.Users.Entities;

namespace StackDuel.Application.Queries.Users.GetUserAvatarHistory;

internal sealed class GetUserAvatarHistoryHandler(
    IUserReadRepository userReadRepository,
    IUserAvatarWriteRepository userAvatarRepository
) : IQueryHandler<GetUserAvatarHistoryQuery, IReadOnlyList<UserAvatarDto>>
{
    public async Task<Result<IReadOnlyList<UserAvatarDto>>> Handle(
        GetUserAvatarHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        UserDto? user = await userReadRepository.FindByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound();

        IReadOnlyList<UserAvatar> avatars = await userAvatarRepository.GetAllByUserIdNewestFirstAsync(
            request.UserId,
            cancellationToken
        );

        IReadOnlyList<UserAvatarDto> dtos =
        [
            .. avatars.Select(a => new UserAvatarDto(
                a.Id,
                a.ImageUrl.Value,
                a.CreatedAt,
                a.ImageUrl.Value == user.ImageUrl
            )),
        ];

        return Result.Success(dtos);
    }
}