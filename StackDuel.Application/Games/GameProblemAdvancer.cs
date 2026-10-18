using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

internal sealed class GameProblemAdvancer(
    IGameProblemSequencer gameProblemSequencer,
    IGameExpiryCanceller gameExpiryCanceller,
    IProblemSelectionStrategyResolver strategyResolver,
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository
) : IGameProblemAdvancer
{
    public async Task<GameProblemAdvanceResult> AdvanceAsync(
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
        ApplyTransition(game, userId, nextProblemId);

        try
        {
            await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        }
        catch (GameProblemPositionConflictException)
        {
            game =
                await gameReadRepository.FindGameByIdAsync(game.Id, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Game '{game.Id}' was not found while resolving a round conflict."
                );
            nextProblemId =
                game.ProblemIdAtPosition(position)
                ?? throw new InvalidOperationException(
                    $"Game '{game.Id}' has no round at position {position} after losing the round-creation race."
                );
            ApplyTransition(game, userId, nextProblemId);
            await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        }

        if (nextProblemId is null)
            await gameExpiryCanceller.CancelIfScheduledAsync(game, cancellationToken);

        return new GameProblemAdvanceResult(game, nextProblemId);
    }

    private static void ApplyTransition(Game game, Guid userId, Guid? nextProblemId)
    {
        if (nextProblemId is { } problemId)
            game.StartRoundFor(userId, problemId);
        else
            game.FinishProblemsFor(userId);
    }
}