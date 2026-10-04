using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

/// <summary>
/// Cancels a game's scheduled GameTimeExpiredMessage when the game has just completed early —
/// shared by every command handler that can end a game before its clock naturally runs out
/// (ForfeitGame, CompleteProblem via pool exhaustion), so the cancellation try/catch and its
/// best-effort semantics live in exactly one place.
/// </summary>
public interface IGameExpiryCanceller
{
    /// <summary>
    /// No-ops unless <paramref name="game"/> is Completed and still has a scheduled expiry handle
    /// attached — safe to call unconditionally right after any action that might have completed
    /// the game.
    /// </summary>
    Task CancelIfScheduledAsync(Game game, CancellationToken cancellationToken = default);
}