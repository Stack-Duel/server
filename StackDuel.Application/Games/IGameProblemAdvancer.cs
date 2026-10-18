using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

public sealed record GameProblemAdvanceResult(Game Game, Guid? NextProblemId);

public interface IGameProblemAdvancer
{
    Task<GameProblemAdvanceResult> AdvanceAsync(
        Game game,
        Guid userId,
        string gameModeKey,
        CancellationToken cancellationToken = default
    );
}