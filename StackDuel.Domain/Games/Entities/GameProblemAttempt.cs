using StackDuel.Domain.Games.Enums;

namespace StackDuel.Domain.Games.Entities;

/// <summary>
/// Outcome of <see cref="Game.EvaluateProblemAttempt"/> — everything CompleteProblemHandler and
/// SkipProblemHandler need to decide what to do next, in one value instead of re-derived
/// independently in each: whether the game happened to auto-complete from expiry as a side effect
/// of the check (the caller must persist that regardless of <see cref="Eligibility"/>), and whether
/// the participant may actually attempt the action.
/// </summary>
public readonly record struct GameProblemAttempt(
    GameParticipant? Participant,
    bool GameJustExpired,
    ProblemAttemptEligibility Eligibility
)
{
    public bool IsEligible => Eligibility == ProblemAttemptEligibility.Eligible;
}