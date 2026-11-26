using Ardalis.Result;
using Moq;
using StackDuel.Application.Configuration;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;
using ReceiveJudge0CallbackCommand = StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback.ReceiveJudge0CallbackCommand;
using ReceiveJudge0CallbackHandler = StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback.ReceiveJudge0CallbackHandler;

namespace StackDuel.Application.Tests.Commands.Submissions.ReceiveJudge0Callback;

public class ReceiveJudge0CallbackHandlerTests
{
    private Mock<IMessagePublisher> _messagePublisher = null!;

    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;

    public ReceiveJudge0CallbackHandlerTests()
    {
        _messagePublisher = new Mock<IMessagePublisher>();
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();
        _submissionReadRepository
            .Setup(x => x.FindSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new SubmissionSummaryDto(Guid.NewGuid(), Guid.NewGuid(), SubmissionType.Submit, SubmissionStatus.Running)
            );
    }

    private ReceiveJudge0CallbackHandler CreateHandler(Judge0Options options) =>
        new(_messagePublisher.Object, _submissionReadRepository.Object, options);

    [Fact]
    public async Task Handle_UseCallbackDisabled_ReturnsUnauthorized()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = false, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("secret", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<SubmissionJobContinuationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WrongKey_ReturnsUnauthorized()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("wrong-key", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<SubmissionJobContinuationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_MissingKey_ReturnsUnauthorized()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand(null, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task Handle_CorrectKey_PublishesContinuationAndSucceeds()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });
        Guid submissionId = Guid.NewGuid();

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("secret", submissionId),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        _messagePublisher.Verify(
            x =>
                x.PublishAsync(
                    It.Is<SubmissionJobContinuationMessage>(m => m.SubmissionId == submissionId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_UnknownSubmission_ReturnsNotFoundAndPublishesNothing()
    {
        _submissionReadRepository
            .Setup(x => x.FindSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubmissionSummaryDto?)null);

        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("secret", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<SubmissionJobContinuationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(SubmissionStatus.Accepted)]
    [InlineData(SubmissionStatus.WrongAnswer)]
    public async Task Handle_AlreadyTerminalSubmission_SucceedsWithoutRepublishing(SubmissionStatus status)
    {
        _submissionReadRepository
            .Setup(x => x.FindSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionSummaryDto(Guid.NewGuid(), Guid.NewGuid(), SubmissionType.Submit, status));

        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("secret", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<SubmissionJobContinuationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_EmptyConfiguredSecret_NeverAuthorizesEvenWithEmptyKey()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }
}