using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Domain.Tests.Games.Entities;

public class GameRoundResultTests
{
    private static readonly DateTime StartedAt = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Solved_RecordsOutcomeAndSolvingSubmission()
    {
        var roundId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        GameRoundResult result = GameRoundResult.Solved(roundId, StartedAt, submissionId);

        Assert.Equal(roundId, result.RoundId);
        Assert.Equal(GameRoundOutcome.Solved, result.Outcome);
        Assert.Equal(StartedAt, result.StartedAt);
        Assert.NotNull(result.Solution);
        Assert.Equal(submissionId, result.SolvingSubmissionId);
    }

    /// <summary>
    /// The solving submission is required rather than a nullable column guarded by a check, so an
    /// unusable id has to be rejected at construction.
    /// </summary>
    [Fact]
    public void Solved_EmptySubmissionId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameRoundResult.Solved(Guid.NewGuid(), StartedAt, Guid.Empty));
    }

    [Fact]
    public void Skipped_RecordsOutcomeWithNoSolution()
    {
        var roundId = Guid.NewGuid();

        GameRoundResult result = GameRoundResult.Skipped(roundId, StartedAt);

        Assert.Equal(roundId, result.RoundId);
        Assert.Equal(GameRoundOutcome.Skipped, result.Outcome);
        Assert.Null(result.Solution);
        Assert.Null(result.SolvingSubmissionId);
    }

    [Fact]
    public void Skipped_EmptyRoundId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameRoundResult.Skipped(Guid.Empty, StartedAt));
    }

    [Fact]
    public void Solved_EmptyRoundId_Throws()
    {
        Assert.Throws<ArgumentException>(() => GameRoundResult.Solved(Guid.Empty, StartedAt, Guid.NewGuid()));
    }

    /// <summary>
    /// A result only exists for a finished round, so its completion time is always set — there is no
    /// in-progress state here to leave it null for.
    /// </summary>
    [Fact]
    public void Skipped_SetsCompletedAt()
    {
        GameRoundResult result = GameRoundResult.Skipped(Guid.NewGuid(), StartedAt);

        Assert.NotEqual(default, result.CompletedAt);
    }
}