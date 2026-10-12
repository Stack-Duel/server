using Ardalis.Result;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Games;

/// <summary>
/// Why a call to <see cref="IGameSubmissionProgressApplier"/> did or didn't move the participant on.
/// <see cref="NotApplicable"/> is a success, not a failure: a duplicate event delivery, a superseded
/// submission, or a plain wrong answer all legitimately leave the game exactly as it was.
/// </summary>
public enum GameSubmissionProgressOutcome
{
    Applied,
    NotApplicable,
}

/// <summary>
/// What happened to the caller's already-loaded game during
/// <see cref="IGameSubmissionProgressApplier.ApplyPendingAsync"/>.
/// <see cref="ConflictedNeedsReload"/> means the caller's instance was discarded and they must
/// re-read the aggregate before using it.
/// </summary>
public enum GamePendingProgressOutcome
{
    NoChange,
    Applied,
    ConflictedNeedsReload,
}

public sealed record GameSubmissionProgressResult(
    GameSubmissionProgressOutcome Outcome,
    int? Score,
    Guid? NextProblemId
);

/// <summary>
/// Folds a finished game submission into the game: scores the solve and moves the participant to
/// their next problem, so a game solve costs the client exactly one write.
/// </summary>
public interface IGameSubmissionProgressApplier
{
    /// <summary>
    /// Applies the progress earned by <paramref name="submissionId"/>, resolving the game,
    /// participant and problem from the submission itself. <see cref="GameSubmissionProgressResult.Score"/>
    /// is null when no participant was resolved, so no score is known.
    /// </summary>
    Task<Result<GameSubmissionProgressResult>> ApplyAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Heals an already-loaded, tracked <paramref name="game"/> whose participants have accepted
    /// submissions nobody scored, using the caller's already-fetched
    /// <paramref name="latestSubmissions"/>, and reports what became of that instance.
    /// </summary>
    Task<GamePendingProgressOutcome> ApplyPendingAsync(
        Game game,
        IReadOnlyList<GameProblemSubmissionDto> latestSubmissions,
        CancellationToken cancellationToken = default
    );
}