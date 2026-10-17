namespace StackDuel.Domain.Games.Enums;

public enum ProblemAttemptEligibility
{
    Eligible = 0,
    ParticipantNotFound = 1,
    GameNotRunning = 2,
    ParticipantStopped = 3,
    RoundNotStarted = 4,
    ProblemMismatch = 5,
}