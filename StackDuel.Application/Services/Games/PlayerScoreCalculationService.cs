using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Rankings;
using StackDuel.Domain.Rankings.Entities;

namespace StackDuel.Application.Services.Games;

public interface IPlayerScoreCalculationService
{
    Task<IEnumerable<CalculatedPlayerRankingScoreDto>>
        CalculatePlayerScoresAsync(
            Game game,
            Guid seasonId,
            CancellationToken cancellationToken
        );
}

internal sealed class PlayerScoreCalculationService(
    IPlayerSeasonRatingWriteRepository ratingRepository
) : IPlayerScoreCalculationService
{
    private const double AdjustmentStrength = 32.0;

    async Task<IEnumerable<CalculatedPlayerRankingScoreDto>>
        IPlayerScoreCalculationService.CalculatePlayerScoresAsync(
            Game game,
            Guid seasonId,
            CancellationToken cancellationToken
        )
    {
        ArgumentNullException.ThrowIfNull(game);
        cancellationToken.ThrowIfCancellationRequested();

        if (seasonId == Guid.Empty)
        {
            throw new ArgumentException(
                "Season ID must not be empty.",
                nameof(seasonId)
            );
        }

        if (game.Status != GameStatus.Completed)
        {
            throw new InvalidOperationException(
                "The game must be completed."
            );
        }

        var participants = game.Participants.ToList();

        if (participants.Count < 2)
        {
            throw new InvalidOperationException(
                "Rating calculation requires at least two players."
            );
        }

        // Capture every player's rating before calculating any changes.
        var currentRatings = new Dictionary<Guid, int>();

        foreach (var participant in participants)
        {
            var seasonalRating = await ratingRepository.FindAsync(
                participant.UserId,
                seasonId,
                cancellationToken
            );

            currentRatings.Add(
                participant.UserId,
                seasonalRating?.Rating
                    ?? PlayerSeasonRating.StartingRating
            );
        }

        var results = new List<CalculatedPlayerRankingScoreDto>(
            participants.Count
        );

        foreach (var player in participants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int previousRating = currentRatings[player.UserId];
            double totalAdjustment = 0;

            foreach (var opponent in participants)
            {
                if (player.UserId == opponent.UserId)
                {
                    continue;
                }

                double expectedResult = CalculateExpectedResult(
                    previousRating,
                    currentRatings[opponent.UserId]
                );

                double actualResult = CalculateActualResult(
                    player.Score,
                    opponent.Score
                );

                totalAdjustment += actualResult - expectedResult;
            }

            double averageAdjustment =
                totalAdjustment / (participants.Count - 1);

            int ratingChange = (int)Math.Round(
                AdjustmentStrength * averageAdjustment,
                MidpointRounding.AwayFromZero
            );

            int newRating = checked(previousRating + ratingChange);

            results.Add(
                new CalculatedPlayerRankingScoreDto(
                    player.UserId,
                    previousRating,
                    ratingChange,
                    newRating
                )
            );
        }

        return results;
    }

    private static double CalculateExpectedResult(
        int playerRating,
        int opponentRating
    )
    {
        double ratingDifference =
            ((double)opponentRating - playerRating) / 400.0;

        return 1.0 / (
            1.0 + Math.Pow(10.0, ratingDifference)
        );
    }

    private static double CalculateActualResult(
        int playerScore,
        int opponentScore
    )
    {
        if (playerScore > opponentScore)
        {
            return 1.0;
        }

        if (playerScore == opponentScore)
        {
            return 0.5;
        }

        return 0.0;
    }
}