namespace StackDuel.Application.Games;

public interface IProblemSelectionStrategyResolver
{
    IProblemSelectionStrategy Resolve(string gameModeKey);
}