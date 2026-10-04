using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

/// <summary>
/// Resolves and applies a participant's next problem for a game mode — shared by
/// CompleteProblemHandler and SkipProblemHandler, which differ only in which participant
/// transition to apply on success and whether running out of problems also consumes a skip.
/// </summary>
public interface IGameProblemAdvancer
{
    /// <summary>
    /// Looks up (or generates) the participant's next problem and applies it: calls
    /// <paramref name="onAdvance"/> with the new problem id if the pool still has one, otherwise
    /// calls <paramref name="onFinish"/> (if given), marks the participant finished, and cancels
    /// the game's scheduled expiry if that finish just completed the whole game.
    /// </summary>
    Task<Guid?> AdvanceAsync(
        Game game,
        GameParticipant participant,
        string gameModeKey,
        Action<Guid> onAdvance,
        Action? onFinish,
        CancellationToken cancellationToken = default
    );
}