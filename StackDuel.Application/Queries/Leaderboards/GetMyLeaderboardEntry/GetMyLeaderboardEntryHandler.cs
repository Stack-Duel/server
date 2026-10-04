using StackDuel.Application.Games;
using StackDuel.Application.Leaderboards;
using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Leaderboards.GetMyLeaderboardEntry;

internal sealed class GetMyLeaderboardEntryHandler(
    IGameReadRepository gameReadRepository,
    ILeaderboardReadRepository leaderboardReadRepository
) : IQueryHandler<GetMyLeaderboardEntryQuery, MyLeaderboardEntryDto>
{
    public async Task<Result<MyLeaderboardEntryDto>> Handle(
        GetMyLeaderboardEntryQuery request,
        CancellationToken cancellationToken
    )
    {
        GameMode? gameMode = await gameReadRepository.FindGameModeByKeyAsync(request.GameModeKey, cancellationToken);
        if (gameMode is null)
            return Result<MyLeaderboardEntryDto>.NotFound("Game mode not found.");

        LeaderboardParticipant? participant = await leaderboardReadRepository.FindParticipantForUserAsync(
            gameMode.Id,
            request.TimeLimitInSeconds,
            request.UserId,
            cancellationToken
        );

        if (participant is null)
            return Result<MyLeaderboardEntryDto>.NotFound("No leaderboard entry for this user yet.");

        return Result<MyLeaderboardEntryDto>.Success(new MyLeaderboardEntryDto(participant.HighScore));
    }
}