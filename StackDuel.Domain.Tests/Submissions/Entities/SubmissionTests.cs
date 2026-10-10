using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Events;
using StackDuel.Domain.Submissions.Exceptions;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Domain.Tests.Submissions.Entities;

public class SubmissionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProblemSetupId = Guid.NewGuid();
    private static readonly SourceCode ValidSourceCode = new("int main() {}");
    private static readonly Guid TestCaseId1 = Guid.NewGuid();
    private static readonly Guid TestCaseId2 = Guid.NewGuid();

    private static Submission CreateSubmission(
        SubmissionType type = SubmissionType.Submit,
        IEnumerable<Guid>? testCaseIds = null,
        Guid? gameId = null
    ) => new(UserId, ProblemSetupId, type, ValidSourceCode, testCaseIds ?? [TestCaseId1, TestCaseId2], gameId: gameId);

    [Fact]
    public void Complete_AllAccepted_SetsStatusToAccepted()
    {
        var submission = CreateSubmission();
        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted);
        submission.UpdateResult(TestCaseId2, SubmissionResultStatus.Accepted);

        submission.Complete();

        Assert.Equal(SubmissionStatus.Accepted, submission.Status);
    }

    [Fact]
    public void Complete_AnyNonAccepted_SetsStatusToWrongAnswer()
    {
        var submission = CreateSubmission();
        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted);
        submission.UpdateResult(TestCaseId2, SubmissionResultStatus.WrongAnswer);

        submission.Complete();

        Assert.Equal(SubmissionStatus.WrongAnswer, submission.Status);
    }

    [Theory]
    [InlineData(SubmissionResultStatus.TimeLimitExceeded)]
    [InlineData(SubmissionResultStatus.MemoryLimitExceeded)]
    [InlineData(SubmissionResultStatus.RuntimeError)]
    [InlineData(SubmissionResultStatus.CompileError)]
    public void Complete_AnyFailureStatus_SetsStatusToWrongAnswer(SubmissionResultStatus failureStatus)
    {
        var submission = CreateSubmission();
        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted);
        submission.UpdateResult(TestCaseId2, failureStatus);

        submission.Complete();

        Assert.Equal(SubmissionStatus.WrongAnswer, submission.Status);
    }

    [Fact]
    public void Complete_RaisesSubmissionCompletedDomainEventWithStatusAndGameId()
    {
        var gameId = Guid.NewGuid();
        var submission = CreateSubmission(gameId: gameId);
        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted);
        submission.UpdateResult(TestCaseId2, SubmissionResultStatus.Accepted);
        submission.PopDomainEvents();

        submission.Complete();

        var domainEvent = submission.PopDomainEvents().OfType<SubmissionCompletedDomainEvent>().Single();
        Assert.Equal(UserId, domainEvent.UserId);
        Assert.Equal(submission.Id, domainEvent.SubmissionId);
        Assert.Equal(SubmissionStatus.Accepted, domainEvent.Status);
        Assert.Equal(gameId, domainEvent.GameId);
    }

    [Fact]
    public void Fail_RaisesSubmissionCompletedDomainEventWithWrongAnswerAndGameId()
    {
        var gameId = Guid.NewGuid();
        var submission = CreateSubmission(gameId: gameId);
        submission.PopDomainEvents();

        submission.Fail();

        var domainEvent = submission.PopDomainEvents().OfType<SubmissionCompletedDomainEvent>().Single();
        Assert.Equal(SubmissionStatus.WrongAnswer, domainEvent.Status);
        Assert.Equal(gameId, domainEvent.GameId);
    }

    [Fact]
    public void Complete_WithPendingResult_ThrowsSubmissionNotCompleteException()
    {
        var submission = CreateSubmission();
        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted);

        Assert.Throws<SubmissionNotCompleteException>(() => submission.Complete());
    }

    [Fact]
    public void Complete_WithProcessingResult_ThrowsSubmissionNotCompleteException()
    {
        var submission = CreateSubmission();
        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted);
        submission.UpdateResult(TestCaseId2, SubmissionResultStatus.Processing);

        Assert.Throws<SubmissionNotCompleteException>(() => submission.Complete());
    }

    [Fact]
    public void Constructor_SetsInitialProperties()
    {
        var submission = CreateSubmission(SubmissionType.Run);

        Assert.Equal(UserId, submission.UserId);
        Assert.Equal(ProblemSetupId, submission.ProblemSetupId);
        Assert.Equal(SubmissionType.Run, submission.Type);
        Assert.Equal(ValidSourceCode, submission.SourceCode);
        Assert.Equal(SubmissionStatus.Queued, submission.Status);
        Assert.Null(submission.GameId);
    }

    [Fact]
    public void Constructor_WithGameId_SetsGameId()
    {
        var gameId = Guid.NewGuid();

        var submission = CreateSubmission(gameId: gameId);

        Assert.Equal(gameId, submission.GameId);
    }

    [Fact]
    public void Constructor_CreatesResultPerTestCaseId()
    {
        var submission = CreateSubmission();

        Assert.Equal(2, submission.Results.Count);
    }

    [Fact]
    public void Constructor_AllResultsInitiallyPending()
    {
        var submission = CreateSubmission();

        Assert.True(submission.Results.All(r => r.Status == SubmissionResultStatus.Pending));
    }

    [Fact]
    public void Constructor_WithNoTestCases_ResultsIsEmpty()
    {
        var submission = CreateSubmission(testCaseIds: []);

        Assert.Empty(submission.Results);
    }

    [Fact]
    public void StartRunning_FromQueued_SetsStatusToRunning()
    {
        var submission = CreateSubmission();

        submission.StartRunning();

        Assert.Equal(SubmissionStatus.Running, submission.Status);
    }

    [Fact]
    public void StartRunning_WhenAlreadyRunning_ThrowsInvalidSubmissionStateException()
    {
        var submission = CreateSubmission();
        submission.StartRunning();

        Assert.Throws<InvalidSubmissionStateException>(() => submission.StartRunning());
    }

    [Fact]
    public void UpdateResult_UnknownTestCaseId_ThrowsSubmissionResultNotFoundException()
    {
        var submission = CreateSubmission();

        Assert.Throws<SubmissionResultNotFoundException>(() =>
            submission.UpdateResult(Guid.NewGuid(), SubmissionResultStatus.Accepted)
        );
    }

    [Fact]
    public void UpdateResult_UpdatesMatchingResult()
    {
        var submission = CreateSubmission();

        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted, runtime: 100, memoryUsed: 32);

        var result = submission.Results.First(r => r.TestCaseId == TestCaseId1);
        Assert.Equal(SubmissionResultStatus.Accepted, result.Status);
        Assert.Equal(100, result.Runtime);
        Assert.Equal(32, result.MemoryUsed);
    }

    [Fact]
    public void UpdateResult_Overwrite_UpdatesExistingResult()
    {
        var submission = CreateSubmission();
        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Processing);

        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted, runtime: 50);

        var result = submission.Results.First(r => r.TestCaseId == TestCaseId1);
        Assert.Equal(SubmissionResultStatus.Accepted, result.Status);
    }

    [Fact]
    public void UpdateResult_DoesNotAffectOtherResults()
    {
        var submission = CreateSubmission();

        submission.UpdateResult(TestCaseId1, SubmissionResultStatus.Accepted);

        var other = submission.Results.First(r => r.TestCaseId == TestCaseId2);
        Assert.Equal(SubmissionResultStatus.Pending, other.Status);
    }
}