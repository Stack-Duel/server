using Ardalis.Result;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Games;

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

            ProblemAttemptEligibility.RoundNotStarted => Result<T>.Invalid(
                new ValidationError("CurrentRoundId", "Round not started.")
            ),

            ProblemAttemptEligibility.ProblemMismatch => Result<T>.Invalid(
                new ValidationError("ProblemId", "The specified problem is not the participant's current problem.")
            ),

            _ => throw new ArgumentOutOfRangeException(nameof(attempt), attempt.Eligibility, "Unhandled eligibility."),
        };
    }
}