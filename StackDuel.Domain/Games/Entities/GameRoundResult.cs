using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameRoundResult : Entity
{
    public static GameRoundResult Solved(Guid roundId, DateTime startedAt, Guid submissionId) =>
        new(roundId, startedAt, GameRoundOutcome.Solved) { Solution = new GameRoundSolution(submissionId) };

    public static GameRoundResult Skipped(Guid roundId, DateTime startedAt) =>
        new(roundId, startedAt, GameRoundOutcome.Skipped);

    private GameRoundResult(Guid roundId, DateTime startedAt, GameRoundOutcome outcome)
    {
        if (roundId == Guid.Empty)
            throw new ArgumentException("Round id must not be empty.", nameof(roundId));

        RoundId = roundId;
        StartedAt = startedAt;
        Outcome = outcome;
        CompletedAt = DateTime.UtcNow;
    }

    private GameRoundResult() { }

    public Guid RoundId { get; private set; }

    public GameRoundOutcome Outcome { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime CompletedAt { get; private set; }

    public GameRoundSolution? Solution { get; private set; }

    public Guid? SolvingSubmissionId => Solution?.SubmissionId;
}