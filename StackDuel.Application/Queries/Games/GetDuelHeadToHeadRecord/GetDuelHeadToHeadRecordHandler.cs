using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Queries.Games.GetDuelHeadToHeadRecord;

internal sealed class GetDuelHeadToHeadRecordHandler(IGameReadRepository gameReadRepository)
    : IQueryHandler<GetDuelHeadToHeadRecordQuery, DuelHeadToHeadRecordDto>
{
    public async Task<Result<DuelHeadToHeadRecordDto>> Handle(
        GetDuelHeadToHeadRecordQuery request,
        CancellationToken cancellationToken
    )
    {
        GameMode? duelMode = await gameReadRepository.FindGameModeByKeyAsync("duel", cancellationToken);
        if (duelMode is null)
            return Result<DuelHeadToHeadRecordDto>.NotFound("Duel game mode was not found.");

        IReadOnlyList<Game> completedGames = await gameReadRepository.GetCompletedGamesForUserAsync(
            request.UserId,
            cancellationToken
        );

        var matchups = completedGames.Where(g =>
            g.GameModeId == duelMode.Id && g.Participants.Any(p => p.UserId == request.OpponentId)
        );

        int wins = 0,
            losses = 0,
            draws = 0,
            gamesPlayed = 0;

        foreach (Game game in matchups)
        {
            gamesPlayed++;

            int myScore = game.Participants.First(p => p.UserId == request.UserId).Score;
            int maxScore = game.Participants.Max(p => p.Score);
            int topScorerCount = game.Participants.Count(p => p.Score == maxScore);

            if (myScore != maxScore)
                losses++;
            else if (topScorerCount > 1)
                draws++;
            else
                wins++;
        }

        return Result<DuelHeadToHeadRecordDto>.Success(new DuelHeadToHeadRecordDto(wins, losses, draws, gamesPlayed));
    }
}