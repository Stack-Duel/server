using Ardalis.Result;
using MediatR;
using StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

namespace StackDuel.Application.Events.Game;

public interface IPlayerGameRankingService
{
    Task<Result> CalculatePlayerGameRankingAsync(Guid gameId, CancellationToken cancellationToken);
}

internal sealed class PlayerGameRankingService(IMediator mediator) : IPlayerGameRankingService
{
    async Task<Result> IPlayerGameRankingService.CalculatePlayerGameRankingAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(new CalculatePlayerRankingsFromGameCommand(gameId), cancellationToken);
    }
}
