using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Events.Submissions;
using StackDuel.Application.Notifications;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Events;

namespace StackDuel.Application.Tests.Events.Submissions;

public class SubmissionCompletedDomainEventHandlerTests
{
    private readonly Mock<ISubmissionNotificationService> _submissionNotifications = new();
    private readonly Mock<IGameNotificationService> _gameNotifications = new();
    private readonly SubmissionCompletedDomainEventHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();
    private readonly Guid _gameId = Guid.NewGuid();

    public SubmissionCompletedDomainEventHandlerTests()
    {
        _handler = new SubmissionCompletedDomainEventHandler(
            _submissionNotifications.Object,
            _gameNotifications.Object,
            NullLogger<SubmissionCompletedDomainEventHandler>.Instance
        );
    }

    private Task Handle(Guid? gameId, SubmissionStatus status, SubmissionCompletionReason reason) =>
        _handler.Handle(
            new DomainEventNotification<SubmissionCompletedDomainEvent>(
                new SubmissionCompletedDomainEvent(_userId, _submissionId, status, gameId, reason)
            ),
            CancellationToken.None
        );

    private void VerifyNoGameFeedPush() =>
        _gameNotifications.Verify(
            x =>
                x.NotifyGameParticipantAttemptedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<SubmissionStatus>(),
                    It.IsAny<DateTime>()
                ),
            Times.Never
        );

    [Fact]
    public async Task Handle_AlwaysNotifiesTheSubmittingUser()
    {
        await Handle(null, SubmissionStatus.Accepted, SubmissionCompletionReason.Graded);

        _submissionNotifications.Verify(
            x => x.NotifySubmissionCompletedAsync(_userId, _submissionId),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_DoesNotPushToTheGameFeedForASubmissionOutsideAGame()
    {
        await Handle(null, SubmissionStatus.Accepted, SubmissionCompletionReason.Graded);

        VerifyNoGameFeedPush();
    }

    [Theory]
    [InlineData(SubmissionStatus.Accepted)]
    [InlineData(SubmissionStatus.WrongAnswer)]
    public async Task Handle_PushesAGradedGameSubmissionToTheActivityFeed(SubmissionStatus status)
    {
        await Handle(_gameId, status, SubmissionCompletionReason.Graded);

        _gameNotifications.Verify(
            x => x.NotifyGameParticipantAttemptedAsync(_gameId, _userId, status, It.IsAny<DateTime>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_DoesNotPushAnExecutionFailureToTheActivityFeed()
    {
        await Handle(_gameId, SubmissionStatus.WrongAnswer, SubmissionCompletionReason.ExecutionFailed);

        VerifyNoGameFeedPush();
    }

    [Fact]
    public async Task Handle_StillPushesTheGameFeedWhenTheSubmissionPushFails()
    {
        _submissionNotifications
            .Setup(x => x.NotifySubmissionCompletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("transport down"));

        await Handle(_gameId, SubmissionStatus.Accepted, SubmissionCompletionReason.Graded);

        _gameNotifications.Verify(
            x =>
                x.NotifyGameParticipantAttemptedAsync(
                    _gameId,
                    _userId,
                    SubmissionStatus.Accepted,
                    It.IsAny<DateTime>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_SwallowsAGameFeedPushFailure()
    {
        _gameNotifications
            .Setup(x =>
                x.NotifyGameParticipantAttemptedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<SubmissionStatus>(),
                    It.IsAny<DateTime>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("transport down"));

        Exception? exception = await Record.ExceptionAsync(
            () => Handle(_gameId, SubmissionStatus.Accepted, SubmissionCompletionReason.Graded)
        );

        Assert.Null(exception);
    }
}