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
        GameParticipant participant,
        string gameModeKey,
        Action<Guid> onAdvance,
        Action? onFinish,
        CancellationToken cancellationToken = default
    )
    {
        IProblemSelectionStrategy strategy = strategyResolver.Resolve(gameModeKey);
        int nextPosition = game.RoundsReachedFor(participant.UserId);

        Guid? nextProblemId = await gameProblemSequencer.GetOrGenerateProblemAsync(
            game,
            nextPosition,
            gameModeKey,
            strategy,
            cancellationToken
        );

        if (nextProblemId is { } problemId)
        {
            onAdvance(problemId);
        }
        else
        {
            onFinish?.Invoke();
            game.FinishProblemsFor(participant.UserId);
            await gameExpiryCanceller.CancelIfScheduledAsync(game, cancellationToken);
        }

        return nextProblemId;
    }
}