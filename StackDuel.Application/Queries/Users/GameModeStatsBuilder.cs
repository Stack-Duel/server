using StackDuel.Application.Games;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Queries.Users;

internal sealed record GameModeStatsResult(
    IReadOnlyDictionary<Guid, GameMode> GameModesById,
    IReadOnlyList<Game> GamesWithKnownMode,
    IReadOnlyList<GameModeStatDto> Stats
);

internal static class GameModeStatsBuilder
{
    public static async Task<GameModeStatsResult> BuildAsync(
        IGameReadRepository gameReadRepository,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Game> completedGames = await gameReadRepository.GetCompletedGamesForUserAsync(
            userId,
            cancellationToken
        );
        IReadOnlyList<GameMode> activeGameModes = await gameReadRepository.GetActiveGameModesAsync(cancellationToken);

        // Seed with every currently-active mode (so an unplayed mode still shows a zeroed-out
        // row), then fill in any mode from the user's history that's since been deactivated —
        // they should still see stats for a mode they actually played.
        Dictionary<Guid, GameMode> gameModesById = activeGameModes.ToDictionary(m => m.Id);
        foreach (Guid gameModeId in completedGames.Select(g => g.GameModeId).Distinct())
        {
            if (gameModesById.ContainsKey(gameModeId))
                continue;

            GameMode? gameMode = await gameReadRepository.FindGameModeByIdIncludingInactiveAsync(
                gameModeId,
                cancellationToken
            );
            if (gameMode is not null)
                gameModesById[gameModeId] = gameMode;
        }

        var gamesWithKnownMode = completedGames.Where(g => gameModesById.ContainsKey(g.GameModeId)).ToList();
        var gamesByModeId = gamesWithKnownMode.GroupBy(g => g.GameModeId).ToDictionary(g => g.Key, g => g.ToList());

        var stats = gameModesById
            .Values.Select(mode => BuildGameModeStat(mode, gamesByModeId.GetValueOrDefault(mode.Id), userId))
            .OrderByDescending(s => s.GamesPlayed)
            .ThenBy(s => s.GameModeName)
            .ToList();

        return new GameModeStatsResult(gameModesById, gamesWithKnownMode, stats);
    }

    private static GameModeStatDto BuildGameModeStat(GameMode mode, List<Game>? games, Guid userId)
    {
        // A mode capped at one player (Solo Rush) has no opponent to win or lose against —
        // it's just a high-score chase, so only track the best score for it.
        bool hasOpponents = mode.MaxPlayers > 1;

        int gamesPlayed = 0,
            wins = 0,
            losses = 0,
            draws = 0,
            bestScore = 0;

        if (games is not null)
        {
            foreach (Game game in games)
            {
                gamesPlayed++;

                int myScore = game.Participants.First(p => p.UserId == userId).Score;
                bestScore = Math.Max(bestScore, myScore);

                if (!hasOpponents)
                    continue;

                int maxScore = game.Participants.Max(p => p.Score);
                int topScorerCount = game.Participants.Count(p => p.Score == maxScore);

                if (myScore != maxScore)
                    losses++;
                else if (topScorerCount > 1)
                    draws++;
                else
                    wins++;
            }
        }

        return new GameModeStatDto(mode.Key, mode.Name, hasOpponents, gamesPlayed, wins, losses, draws, bestScore);
    }
}