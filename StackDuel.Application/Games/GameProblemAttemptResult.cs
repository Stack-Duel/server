using Ardalis.Result;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Games;

/// <summary>
/// Translates a <see cref="GameProblemAttempt"/> that failed eligibility into the Result each
/// handler needs to return — shared by CompleteProblemHandler and SkipProblemHandler so the four
/// failure messages live in exactly one place instead of copy-pasted in both.
/// </summary>
internal static class GameProblemAttemptResult
{
    public static Result<T> ToFailure<T>(GameProblemAttempt attempt, ParticipantAction action)
    {
        return attempt.Eligibility switch
        {
            ProblemAttemptEligibility.ParticipantNotFound => Result<T>.Forbidden(),

            ProblemAttemptEligibility.GameNotRunning => Result<T>.Invalid(
                new ValidationError(
                    "Status",
                    action == ParticipantAction.SkipProblem
                        ? "Only running games can have problems skipped."
                        : "Only running games can have problems completed."
                )
            ),

            ProblemAttemptEligibility.ParticipantStopped => Result<T>.Invalid(
                new ValidationError(
                    nameof(GameParticipant.HasStoppedPlaying),
                    attempt.Participant!.HasForfeited
                        ? "You have forfeited this game."
                        : "You have already completed all available problems."
                )
            ),

            ProblemAttemptEligibility.ProblemSessionNotInitialized => Result<T>.Invalid(
                new ValidationError(nameof(GameParticipant.ProblemSession), "Problem session not initialized.")
            ),

            ProblemAttemptEligibility.ProblemMismatch => Result<T>.Invalid(
                new ValidationError("ProblemId", "The specified problem is not the participant's current problem.")
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(attempt), attempt.Eligibility, "Unhandled eligibility."),
        };
    }
}