using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

public interface IGameProblemAdvancer
{
    Task<Guid?> AdvanceAsync(
        Game game,
        Guid userId,
        string gameModeKey,
        CancellationToken cancellationToken = default
    );
}