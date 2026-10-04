namespace StackDuel.Application.Games;

public sealed record ProblemSelectionContext(
    Guid GameId,
    Guid PoolId,
    string GameModeKey,
    int RoundIndex,
    IReadOnlyCollection<Guid> ExcludedProblemIds,
    IReadOnlyCollection<Guid> AllowedLanguageVersionIds
);

public interface IProblemSelectionStrategy
{
    string GameModeKey { get; }

    Task<Guid?> SelectNextProblemIdAsync(
        ProblemSelectionContext context,
        CancellationToken cancellationToken = default
    );
}