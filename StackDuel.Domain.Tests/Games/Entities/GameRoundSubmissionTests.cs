using StackDuel.Domain.Games.Entities;

namespace StackDuel.Domain.Tests.Games.Entities;

public class GameRoundSubmissionTests
{
    private static Guid ValidRoundId => Guid.NewGuid();
    private static Guid ValidParticipantId => Guid.NewGuid();
    private static Guid ValidSetupId => Guid.NewGuid();
    private static Guid ValidSubmissionId => Guid.NewGuid();
    private static Guid ValidProblemId => Guid.NewGuid();
    private static Guid ValidGameId => Guid.NewGuid();
    private static Guid ValidUserId => Guid.NewGuid();

    [Fact]
    public void Constructor_EmptyRoundId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new GameRoundSubmission(
                    Guid.Empty,
                    ValidParticipantId,
                    ValidSetupId,
                    ValidSubmissionId,
                    ValidProblemId,
                    ValidGameId,
                    ValidUserId
                )
        );
    }

    [Fact]
    public void Constructor_EmptyParticipantId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new GameRoundSubmission(
                    ValidRoundId,
                    Guid.Empty,
                    ValidSetupId,
                    ValidSubmissionId,
                    ValidProblemId,
                    ValidGameId,
                    ValidUserId
                )
        );
    }

    [Fact]
    public void Constructor_EmptySetupId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new GameRoundSubmission(
                    ValidRoundId,
                    ValidParticipantId,
                    Guid.Empty,
                    ValidSubmissionId,
                    ValidProblemId,
                    ValidGameId,
                    ValidUserId
                )
        );
    }

    [Fact]
    public void Constructor_EmptySubmissionId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new GameRoundSubmission(
                    ValidRoundId,
                    ValidParticipantId,
                    ValidSetupId,
                    Guid.Empty,
                    ValidProblemId,
                    ValidGameId,
                    ValidUserId
                )
        );
    }

    [Fact]
    public void Constructor_EmptyProblemId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new GameRoundSubmission(
                    ValidRoundId,
                    ValidParticipantId,
                    ValidSetupId,
                    ValidSubmissionId,
                    Guid.Empty,
                    ValidGameId,
                    ValidUserId
                )
        );
    }

    [Fact]
    public void Constructor_EmptyGameId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new GameRoundSubmission(
                    ValidRoundId,
                    ValidParticipantId,
                    ValidSetupId,
                    ValidSubmissionId,
                    ValidProblemId,
                    Guid.Empty,
                    ValidUserId
                )
        );
    }

    [Fact]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new GameRoundSubmission(
                    ValidRoundId,
                    ValidParticipantId,
                    ValidSetupId,
                    ValidSubmissionId,
                    ValidProblemId,
                    ValidGameId,
                    Guid.Empty
                )
        );
    }

    [Fact]
    public void Constructor_AllValid_AssignsFields()
    {
        Guid roundId = ValidRoundId;
        Guid participantId = ValidParticipantId;
        Guid setupId = ValidSetupId;
        Guid submissionId = ValidSubmissionId;
        Guid problemId = ValidProblemId;
        Guid gameId = ValidGameId;
        Guid userId = ValidUserId;

        var roundSubmission = new GameRoundSubmission(
            roundId,
            participantId,
            setupId,
            submissionId,
            problemId,
            gameId,
            userId
        );

        Assert.Equal(roundId, roundSubmission.RoundId);
        Assert.Equal(participantId, roundSubmission.ParticipantId);
        Assert.Equal(setupId, roundSubmission.SetupId);
        Assert.Equal(submissionId, roundSubmission.SubmissionId);
        Assert.Equal(problemId, roundSubmission.ProblemId);
        Assert.Equal(gameId, roundSubmission.GameId);
        Assert.Equal(userId, roundSubmission.UserId);
    }
}