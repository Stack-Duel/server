using StackDuel.Application.Games;
using StackDuel.Application.Leaderboards;
using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Leaderboards.GetLeaderboard;

internal sealed class GetLeaderboardHandler(
    IGameReadRepository gameReadRepository,
    ILeaderboardReadRepository leaderboardReadRepository,
    IUserReadRepository userReadRepository
) : IQueryHandler<GetLeaderboardQuery, PageResult<LeaderboardEntryDto>>
{
    public async Task<Result<PageResult<LeaderboardEntryDto>>> Handle(
        GetLeaderboardQuery request,
        CancellationToken cancellationToken
    )
    {
        GameMode? gameMode = await gameReadRepository.FindGameModeByKeyAsync(request.GameModeKey, cancellationToken);
        if (gameMode is null)
            return Result<PageResult<LeaderboardEntryDto>>.NotFound("Game mode not found.");

        PageResult<LeaderboardParticipant> page = await leaderboardReadRepository.GetRankingsAsync(
            gameMode.Id,
            request.TimeLimitInSeconds,
            request.PaginationRequest,
            cancellationToken
        );

        IReadOnlyDictionary<Guid, UserDto> usersById = await userReadRepository.FindByIdsAsync(
            page.Results.Select(p => p.UserId),
            cancellationToken
        );

        List<LeaderboardEntryDto> entries = page
            .Results.Select(
                (participant, index) =>
                {
                    usersById.TryGetValue(participant.UserId, out UserDto? user);

                    return new LeaderboardEntryDto(
                        page.Offset + index + 1,
                        participant.UserId,
                        user?.Username ?? string.Empty,
                        user?.ImageUrl,
                        participant.HighScore,
                        request.RequestedByUserId == participant.UserId
                    );
                }
            )
            .ToList();

        return Result<PageResult<LeaderboardEntryDto>>.Success(
            new PageResult<LeaderboardEntryDto>
            {
                Results = entries,
                Total = page.Total,
                Page = page.Page,
                Size = page.Size,
            }
        );
    }
}