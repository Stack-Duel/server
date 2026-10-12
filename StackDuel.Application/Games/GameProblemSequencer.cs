using StackDuel.Application.Languages;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

internal sealed class GameProblemSequencer(ILanguageReadRepository languageReadRepository) : IGameProblemSequencer
{
    public async Task<Guid?> GetOrGenerateProblemAsync(
        Game game,
        int position,
        string gameModeKey,
        IProblemSelectionStrategy strategy,
        CancellationToken cancellationToken = default
    )
    {
        Guid? existingProblemId = game.ProblemIdAtPosition(position);
        if (existingProblemId.HasValue)
            return existingProblemId;

        Guid[] selectedLanguageIds = [.. game.Tracks.SelectMany(t => t.Languages).Select(l => l.LanguageId).Distinct()];
        IReadOnlyList<Guid> allowedLanguageVersionIds =
            await languageReadRepository.GetLanguageVersionIdsByLanguageIdsAsync(
                selectedLanguageIds,
                cancellationToken
            );

        Guid[] usedProblemIds = [.. game.ProblemSequence.Select(p => p.ProblemId)];
        var context = new ProblemSelectionContext(
            game.Id,
            game.PoolId,
            gameModeKey,
            position,
            usedProblemIds,
            allowedLanguageVersionIds
        );

        Guid? generatedProblemId = await strategy.SelectNextProblemIdAsync(context, cancellationToken);
        if (generatedProblemId.HasValue)
            game.AppendProblem(generatedProblemId.Value);

        return generatedProblemId;
    }
}