using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Services.Games;

public interface IPlayerScoreCalculationService
{
    Task<IEnumerable<CalculatedPlayerRankingScoreDto>> CalculatePlayerScoresAsync(Game game, CancellationToken cancellationToken);
}

internal sealed class PlayerScoreCalculationService : IPlayerScoreCalculationService
{
    Task<IEnumerable<CalculatedPlayerRankingScoreDto>> IPlayerScoreCalculationService.CalculatePlayerScoresAsync(Game game, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}