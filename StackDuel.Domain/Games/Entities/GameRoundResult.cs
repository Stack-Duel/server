using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

/// <summary>
/// How one participant finished with one round. Exists only for rounds they have actually finished:
/// the round a participant is currently on is held by
/// <see cref="GameParticipant.CurrentRoundId"/> instead, which is what lets every column here be
/// required rather than nullable-until-completed.
/// </summary>
/// <remarks>
/// One row per outcome, discriminated by <see cref="Outcome"/>. An outcome needing data of its own
/// gets a table keyed on (id, outcome) — see <see cref="GameRoundSolution"/> — so such a row can
/// only attach to a result already carrying that outcome, and two of them can never attach to the
/// same result.
/// </remarks>
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

    /// <summary>
    /// Present exactly for <see cref="GameRoundOutcome.Solved"/>, and required when present — a
    /// solved round always names the submission that solved it.
    /// </summary>
    public GameRoundSolution? Solution { get; private set; }

    public Guid? SolvingSubmissionId => Solution?.SubmissionId;
}