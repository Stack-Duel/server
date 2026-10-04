using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

/// <summary>
/// Resolves the problem a participant should get for a given position in a game's shared
/// sequence — reusing it if some other participant already reached that position, or
/// generating and recording a new one (restricted to the game's selected track languages) if
/// this is the first arrival. Callers (StartGame/CompleteProblem/SkipProblem) are responsible
/// for persisting the game afterward; this only mutates the in-memory aggregate.
/// </summary>
public interface IGameProblemSequencer
{
    Task<Guid?> GetOrGenerateProblemAsync(
        Game game,
        int position,
        string gameModeKey,
        IProblemSelectionStrategy strategy,
        CancellationToken cancellationToken = default
    );
}