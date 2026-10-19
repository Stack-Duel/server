using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;
using StackDuel.Application.Games.Dtos;

namespace StackDuel.Application.Services.Games;

public interface IPlayerGameRankingService
{
    Task<Result<IEnumerable<CalculatedPlayerRankingScoreDto>>>
        CalculatePlayerGameRankingAsync(
            Guid gameId,
            Guid seasonId,
            CancellationToken cancellationToken
        );
}

internal sealed class PlayerGameRankingService(
    IMediator mediator
) : IPlayerGameRankingService
{
    async Task<Result<IEnumerable<CalculatedPlayerRankingScoreDto>>>
        IPlayerGameRankingService.CalculatePlayerGameRankingAsync(
            Guid gameId,
            Guid seasonId,
            CancellationToken cancellationToken
        )
    {
        return await mediator.Send(
            new CalculatePlayerRankingsFromGameCommand(
                gameId,
                seasonId
            ),
            cancellationToken
        );
    }
}