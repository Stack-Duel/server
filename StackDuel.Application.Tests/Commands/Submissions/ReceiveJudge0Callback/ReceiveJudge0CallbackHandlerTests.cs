using StackDuel.Application.Configuration;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using Ardalis.Result;
using Moq;
using ReceiveJudge0CallbackCommand = StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback.ReceiveJudge0CallbackCommand;
using ReceiveJudge0CallbackHandler = StackDuel.Application.Commands.Submissions.ReceiveJudge0Callback.ReceiveJudge0CallbackHandler;

namespace StackDuel.Application.Tests.Commands.Submissions.ReceiveJudge0Callback;

public class ReceiveJudge0CallbackHandlerTests
{
    private Mock<IMessagePublisher> _messagePublisher = null!;

    [SetUp]
    public void SetUp()
    {
        _messagePublisher = new Mock<IMessagePublisher>();
    }

    private ReceiveJudge0CallbackHandler CreateHandler(Judge0Options options) => new(_messagePublisher.Object, options);

    [Test]
    public async Task Handle_UseCallbackDisabled_ReturnsUnauthorized()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = false, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("secret", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Unauthorized));
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<SubmissionJobContinuationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_WrongKey_ReturnsUnauthorized()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("wrong-key", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Unauthorized));
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<SubmissionJobContinuationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_MissingKey_ReturnsUnauthorized()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand(null, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Unauthorized));
    }

    [Test]
    public async Task Handle_CorrectKey_PublishesContinuationAndSucceeds()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "secret" });
        Guid submissionId = Guid.NewGuid();

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("secret", submissionId),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        _messagePublisher.Verify(
            x =>
                x.PublishAsync(
                    It.Is<SubmissionJobContinuationMessage>(m => m.SubmissionId == submissionId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_EmptyConfiguredSecret_NeverAuthorizesEvenWithEmptyKey()
    {
        var handler = CreateHandler(new Judge0Options { UseCallback = true, CallbackSecret = "" });

        Result result = await handler.Handle(
            new ReceiveJudge0CallbackCommand("", Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Unauthorized));
    }
}