using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Notifications;

/// <summary>
/// Pushes real-time game-lifecycle notifications to connected clients. The Application layer
/// only knows it needs to announce "this game is now in this state" — the actual transport
/// (SignalR, or anything else in the future) is an Api/Infrastructure concern.
/// </summary>
public interface IGameNotificationService
{
    /// <summary>
    /// Notifies whoever is watching the given game that it has reached a terminal state.
    /// Best-effort: a client that never connects, or a transient push failure, doesn't affect
    /// game correctness — it's purely a latency optimization over polling.
    /// </summary>
    Task NotifyGameCompletedAsync(
        Guid gameId,
        GameStatus status,
        DateTime endedAt,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Notifies whoever is sitting in a Pending lobby that its participant roster changed — someone
    /// joined or left. Same best-effort semantics as <see cref="NotifyGameCompletedAsync"/>: purely
    /// a latency optimization, never a correctness dependency.
    /// </summary>
    Task NotifyGameLobbyUpdatedAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies whoever is watching a Running game that a participant's progress changed (they
    /// solved a problem). Same best-effort semantics as <see cref="NotifyGameCompletedAsync"/>.
    /// </summary>
    Task NotifyGameProgressUpdatedAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies whoever is watching a Running game that a participant's submission just reached a
    /// terminal state (Accepted or WrongAnswer) — powers the live activity feed. Same best-effort
    /// semantics as <see cref="NotifyGameCompletedAsync"/>, except there's no REST fallback here:
    /// a missed push is just a missing feed entry, not stale data a poll would later correct.
    /// </summary>
    Task NotifyGameParticipantAttemptedAsync(
        Guid gameId,
        Guid userId,
        SubmissionStatus status,
        DateTime attemptedAt,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Notifies whoever is watching a Running game that a participant sent a quick emoji
    /// reaction. Same best-effort semantics as <see cref="NotifyGameCompletedAsync"/>, and same
    /// "no REST fallback" caveat as <see cref="NotifyGameParticipantAttemptedAsync"/> — a missed
    /// push is just a missing bubble in the chat, not stale data.
    /// </summary>
    Task NotifyGameParticipantReactedAsync(
        Guid gameId,
        Guid userId,
        string emoji,
        DateTime sentAt,
        CancellationToken cancellationToken = default
    );
}