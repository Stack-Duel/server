namespace StackDuel.Application.Games;

internal sealed class ProblemSelectionStrategyResolver(IEnumerable<IProblemSelectionStrategy> strategies)
    : IProblemSelectionStrategyResolver
{
    public IProblemSelectionStrategy Resolve(string gameModeKey)
    {
        IProblemSelectionStrategy? strategy = strategies.FirstOrDefault(s =>
            s.GameModeKey.Equals(gameModeKey, StringComparison.OrdinalIgnoreCase)
        );

        return strategy
            ?? throw new InvalidOperationException(
                $"No problem selection strategy registered for game mode '{gameModeKey}'."
            );
    }
}