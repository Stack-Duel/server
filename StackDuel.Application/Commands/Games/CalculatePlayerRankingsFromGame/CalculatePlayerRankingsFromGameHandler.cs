using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Services.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

internal sealed class CalculatePlayerRankingsFromGameHandler(
    IPlayerScoreCalculationService playerScoreCalculationService,
    IGameWriteRepository gameWriteRepository,
    IValidator<CalculatePlayerRankingsFromGameCommand> validator
) : AbstractCommandHandler<
    CalculatePlayerRankingsFromGameCommand,
    IEnumerable<CalculatedPlayerRankingScoreDto>
>(validator)
{
    protected override async Task<
        Result<IEnumerable<CalculatedPlayerRankingScoreDto>>
    > HandleValidated(
        CalculatePlayerRankingsFromGameCommand request,
        CancellationToken cancellationToken
    )
    {
        var game = await gameWriteRepository.FindByIdAsync(
            request.GameId,
            cancellationToken
        );

        if (game is null)
        {
            return Result.NotFound(
                $"Game with ID {request.GameId} not found."
            );
        }

        if (game.Status != GameStatus.Completed)
        {
            return Result.Error(
                "Ratings can only be calculated for completed games."
            );
        }

        if (game.Participants.Count < 2)
        {
            return Result.Error(
                "Rating calculation requires at least two players."
            );
        }

        var ratings =
            await playerScoreCalculationService.CalculatePlayerScoresAsync(
                game,
                request.SeasonId,
                cancellationToken
            );

        return Result.Success(ratings);
    }
}