using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

internal sealed class GameProblemAdvancer(
    IGameProblemSequencer gameProblemSequencer,
    IGameExpiryCanceller gameExpiryCanceller,
    IProblemSelectionStrategyResolver strategyResolver
) : IGameProblemAdvancer
{
    public async Task<Guid?> AdvanceAsync(
        Game game,
        Guid userId,
        string gameModeKey,
        CancellationToken cancellationToken = default
    )
    {
        int position = game.RoundsReachedFor(userId);
        IProblemSelectionStrategy strategy = strategyResolver.Resolve(gameModeKey);
        Guid? nextProblemId = await gameProblemSequencer.GetOrGenerateProblemAsync(
            game,
            position,
            gameModeKey,
            strategy,
            cancellationToken
        );

        if (nextProblemId is { } problemId)
        {
            game.StartRoundFor(userId, problemId);
        }
        else
        {
            game.FinishProblemsFor(userId);
            await gameExpiryCanceller.CancelIfScheduledAsync(game, cancellationToken);
        }

        return nextProblemId;
    }
}