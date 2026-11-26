using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Queries.Games.GetGameAccess;

internal sealed class GetGameAccessHandler(IGameReadRepository gameReadRepository)
    : IQueryHandler<GetGameAccessQuery, GameAccessDto>
{
    public async Task<Result<GameAccessDto>> Handle(GetGameAccessQuery request, CancellationToken cancellationToken)
    {
        if (request.GameId == Guid.Empty || request.RequestedByUserId == Guid.Empty)
            return Result<GameAccessDto>.NotFound();

        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result<GameAccessDto>.NotFound();

        if (!game.Participants.Any(p => p.UserId == request.RequestedByUserId))
            return Result<GameAccessDto>.Forbidden();

        return Result<GameAccessDto>.Success(new GameAccessDto(game.Status));
    }
}