using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Application.Queries.Users.GetUserGameStats;

internal sealed class GetUserGameStatsHandler(
    IUserReadRepository userReadRepository,
    IGameReadRepository gameReadRepository
) : IQueryHandler<GetUserGameStatsQuery, UserGameStatsDto>
{
    public async Task<Result<UserGameStatsDto>> Handle(
        GetUserGameStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        var profile = await userReadRepository.FindProfileByUsernameAsync(request.Username, cancellationToken);

        if (profile is null)
            return Result.NotFound();

        bool isOwnProfile = request.RequestingUserId is not null && request.RequestingUserId == profile.Id;
        bool showPrivateSections = isOwnProfile || !profile.IsPrivate;

        if (!showPrivateSections)
            return Result.Success(new UserGameStatsDto(profile.Id, profile.Username, isOwnProfile, null));

        GameModeStatsResult statsResult = await GameModeStatsBuilder.BuildAsync(
            gameReadRepository,
            profile.Id,
            cancellationToken
        );

        return Result.Success(new UserGameStatsDto(profile.Id, profile.Username, isOwnProfile, statsResult.Stats));
    }
}