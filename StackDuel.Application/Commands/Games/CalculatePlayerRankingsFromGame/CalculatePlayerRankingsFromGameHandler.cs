
using Ardalis.Result;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Services.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

internal sealed partial class CalculatePlayerRankingsFromGameHandler(IPlayerScoreCalculationService playerScoreCalculationService, IGameWriteRepository gameWriteRepository) : AbstractCommandHandler<CalculatePlayerRankingsFromGameCommand, IEnumerable<CalculatedPlayerRankingScoreDto>>
{
    protected override async Task<Result<IEnumerable<CalculatedPlayerRankingScoreDto>>> HandleValidated(

        CalculatePlayerRankingsFromGameCommand request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameWriteRepository.FindByIdAsync(request.GameId, cancellationToken);

        // if the game is null, return a not found result

        // validate that the game is actually over and that the player rankings can be calculated, and haven't already been calculated.

        if (game is null) {
            return Result.NotFound($"Game with ID {request.GameId} not found.");
        }

        // Implement the logic to calculate player rankings from the game here.
        return Result.Success(await playerScoreCalculationService.CalculatePlayerScoresAsync(game, cancellationToken));
    }
}   