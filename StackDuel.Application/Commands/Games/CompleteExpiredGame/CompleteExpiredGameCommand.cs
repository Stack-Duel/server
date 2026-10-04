using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Games.CompleteExpiredGame;

/// <summary>
/// Triggered by the GameTimeExpiredMessage consumer (or the sweep job) — not by a player
/// action, so there's no RequestedByUserId/permission check here.
/// </summary>
public sealed record CompleteExpiredGameCommand(Guid GameId, DateTime ExpectedStartedAt, int RescheduleCount)
    : ICommand<CompleteExpiredGameResult>;

public enum CompleteExpiredGameOutcome
{
    Completed,
    NotYetExpired_Rescheduled,
    NotYetExpired_RescheduleLimitReached_CompletedAnyway,
    NotYetExpired_ImplausibleDelay_Dropped,
    Stale_Dropped,
    AlreadyFinalized_NoOp,
}

public sealed record CompleteExpiredGameResult(CompleteExpiredGameOutcome Outcome, DateTime? RescheduleForUtc = null);