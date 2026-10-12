using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Submissions;
using StackDuel.Application.Tests.Support;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Events;
using StackDuel.Domain.Submissions.ValueObjects;

namespace StackDuel.Application.Tests.Submissions;

public class SubmissionFailureServiceTests
{
    private readonly Mock<ISubmissionWriteRepository> _submissionWriteRepository = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcher = new();
    private readonly RecordingLogger<SubmissionFailureService> _logger = new();
    private readonly SubmissionFailureService _service;

    public SubmissionFailureServiceTests()
    {
        _service = new SubmissionFailureService(
            _submissionWriteRepository.Object,
            _domainEventDispatcher.Object,
            _logger
        );
    }

    private static Submission CreateQueuedSubmission() =>
        new(Guid.NewGuid(), Guid.NewGuid(), SubmissionType.Submit, new SourceCode("print(1)"), []);

    private static Submission CreateGradedSubmission(SubmissionStatus status)
    {
        var testCaseId = Guid.NewGuid();
        var submission = new Submission(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubmissionType.Submit,
            new SourceCode("print(1)"),
            [testCaseId]
        );
        submission.UpdateResult(
            testCaseId,
            status == SubmissionStatus.Accepted ? SubmissionResultStatus.Accepted : SubmissionResultStatus.WrongAnswer
        );
        submission.Complete();
        submission.PopDomainEvents();

        Assert.Equal(status, submission.Status);
        return submission;
    }

    private void GivenSubmission(Submission? submission) =>
        _submissionWriteRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

    private void VerifyNothingPersistedOrAnnounced()
    {
        _submissionWriteRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task MarkFailedAsync_SubmissionNotFound_DoesNothing()
    {
        GivenSubmission(null);

        await _service.MarkFailedAsync(Guid.NewGuid(), CancellationToken.None);

        VerifyNothingPersistedOrAnnounced();
        Assert.Equal(0, _logger.WarningOrErrorCount);
    }

    [Theory]
    [InlineData(SubmissionStatus.Accepted)]
    [InlineData(SubmissionStatus.WrongAnswer)]
    public async Task MarkFailedAsync_SubmissionAlreadyGraded_LeavesTheVerdictIntact(SubmissionStatus status)
    {
        Submission submission = CreateGradedSubmission(status);
        GivenSubmission(submission);

        await _service.MarkFailedAsync(submission.Id, CancellationToken.None);

        Assert.Equal(status, submission.Status);
        VerifyNothingPersistedOrAnnounced();
    }

    [Fact]
    public async Task MarkFailedAsync_NonTerminalSubmission_FailsPersistsAndAnnouncesCompletion()
    {
        Submission submission = CreateQueuedSubmission();
        GivenSubmission(submission);

        IEnumerable<IDomainEvent>? dispatched = null;
        _domainEventDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<IDomainEvent> events, CancellationToken _) => dispatched = events)
            .Returns(Task.CompletedTask);

        await _service.MarkFailedAsync(submission.Id, CancellationToken.None);

        Assert.Equal(SubmissionStatus.WrongAnswer, submission.Status);
        _submissionWriteRepository.Verify(x => x.UpdateAsync(submission, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(dispatched);
        SubmissionCompletedDomainEvent completed = Assert.Single(dispatched!.OfType<SubmissionCompletedDomainEvent>());
        Assert.Equal(submission.Id, completed.SubmissionId);
        Assert.Equal(SubmissionStatus.WrongAnswer, completed.Status);
        Assert.Equal(0, _logger.WarningOrErrorCount);
    }

    [Fact]
    public async Task MarkFailedAsync_PersistenceThrows_SwallowsAndLogs()
    {
        Submission submission = CreateQueuedSubmission();
        GivenSubmission(submission);
        _submissionWriteRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        await _service.MarkFailedAsync(submission.Id, CancellationToken.None);

        Assert.Equal(1, _logger.WarningOrErrorCount);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task MarkFailedAsync_LookupThrows_SwallowsAndLogs()
    {
        _submissionWriteRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        await _service.MarkFailedAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(1, _logger.WarningOrErrorCount);
        VerifyNothingPersistedOrAnnounced();
    }
}
