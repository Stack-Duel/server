using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Domain.Tests.Games.Entities;

public class GameRoundParticipantTests
{
    [Fact]
    public void Constructor_EmptyRoundId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GameRoundParticipant(Guid.Empty));
    }

    [Fact]
    public void Constructor_StartsInProgress_WithNoSubmission()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());

        Assert.True(roundParticipant.IsInProgress);
        Assert.Equal(GameRoundOutcome.InProgress, roundParticipant.Outcome);
        Assert.Null(roundParticipant.SubmissionId);
        Assert.Null(roundParticipant.CompletedAt);
    }

    [Fact]
    public void AttachSubmission_SetsSubmissionId()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());
        Guid submissionId = Guid.NewGuid();

        roundParticipant.AttachSubmission(submissionId);

        Assert.Equal(submissionId, roundParticipant.SubmissionId);
    }

    [Fact]
    public void AttachSubmission_EmptySubmissionId_Throws()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => roundParticipant.AttachSubmission(Guid.Empty));
    }

    [Fact]
    public void AttachSubmission_RoundAlreadyCompleted_Throws()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());
        roundParticipant.MarkSolved();

        Assert.Throws<InvalidOperationException>(() => roundParticipant.AttachSubmission(Guid.NewGuid()));
    }

    [Fact]
    public void HasSubmission_MatchesOnlyTheAttachedSubmission()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());
        Guid submissionId = Guid.NewGuid();
        roundParticipant.AttachSubmission(submissionId);

        Assert.True(roundParticipant.HasSubmission(submissionId));
        Assert.False(roundParticipant.HasSubmission(Guid.NewGuid()));
    }

    [Fact]
    public void HasSubmission_NoSubmissionAttached_ReturnsFalse()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());

        Assert.False(roundParticipant.HasSubmission(Guid.NewGuid()));
    }

    [Fact]
    public void MarkSolved_SetsOutcomeToSolved_AndSetsCompletedAt()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());

        roundParticipant.MarkSolved();

        Assert.Equal(GameRoundOutcome.Solved, roundParticipant.Outcome);
        Assert.NotNull(roundParticipant.CompletedAt);
        Assert.False(roundParticipant.IsInProgress);
    }

    [Fact]
    public void MarkSkipped_SetsOutcomeToSkipped_AndSetsCompletedAt()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());

        roundParticipant.MarkSkipped();

        Assert.Equal(GameRoundOutcome.Skipped, roundParticipant.Outcome);
        Assert.NotNull(roundParticipant.CompletedAt);
        Assert.False(roundParticipant.IsInProgress);
    }

    [Fact]
    public void MarkSolved_AlreadySolved_Throws()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());
        roundParticipant.MarkSolved();

        Assert.Throws<InvalidOperationException>(() => roundParticipant.MarkSolved());
    }

    [Fact]
    public void MarkSkipped_AlreadySkipped_Throws()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());
        roundParticipant.MarkSkipped();

        Assert.Throws<InvalidOperationException>(() => roundParticipant.MarkSkipped());
    }

    [Fact]
    public void MarkSolved_AlreadySkipped_Throws()
    {
        var roundParticipant = new GameRoundParticipant(Guid.NewGuid());
        roundParticipant.MarkSkipped();

        Assert.Throws<InvalidOperationException>(() => roundParticipant.MarkSolved());
    }
}