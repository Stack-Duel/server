using Moq;
using StackDuel.Application.Events;
using StackDuel.Domain.Feedback;
using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.Feedback.Factories;
using SubmitFeedbackCommand = StackDuel.Application.Commands.Feedback.SubmitFeedback.SubmitFeedbackCommand;
using SubmitFeedbackHandler = StackDuel.Application.Commands.Feedback.SubmitFeedback.SubmitFeedbackHandler;
using SubmitFeedbackValidator = StackDuel.Application.Commands.Feedback.SubmitFeedback.SubmitFeedbackValidator;

namespace StackDuel.Application.Tests.Commands.Feedback.SubmitFeedback;

public class SubmitFeedbackHandlerTests
{
    private Mock<IFeedbackWriteRepository> _feedbackRepository = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private SubmitFeedbackHandler _handler = null!;

    public SubmitFeedbackHandlerTests()
    {
        _feedbackRepository = new Mock<IFeedbackWriteRepository>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _handler = new SubmitFeedbackHandler(
            new SubmitFeedbackValidator(),
            new FeedbackSubmissionFactory(),
            _feedbackRepository.Object,
            _domainEventDispatcher.Object
        );
    }

    private static SubmitFeedbackCommand ValidCommand() =>
        new(
            Guid.NewGuid(),
            FeedbackType.Bug,
            "Something is broken.",
            4,
            FeedbackContextType.None,
            null,
            "https://stackduel.dev/problems/two-sum",
            "Mozilla/5.0"
        );

    [Fact]
    public async Task Handle_ValidCommand_AddsFeedbackAndReturnsItsId()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _feedbackRepository.Verify(
            r => r.AddAsync(It.Is<FeedbackSubmission>(f => f.Id == result.Value), CancellationToken.None),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_InvalidCommand_DoesNotAddFeedback()
    {
        var command = ValidCommand() with { Message = "" };

        await _handler.Handle(command, CancellationToken.None);

        _feedbackRepository.Verify(
            r => r.AddAsync(It.IsAny<FeedbackSubmission>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}