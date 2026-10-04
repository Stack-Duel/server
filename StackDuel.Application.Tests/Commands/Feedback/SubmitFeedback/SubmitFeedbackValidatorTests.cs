using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.Feedback.ValueObjects;
using SubmitFeedbackCommand = StackDuel.Application.Commands.Feedback.SubmitFeedback.SubmitFeedbackCommand;
using SubmitFeedbackValidator = StackDuel.Application.Commands.Feedback.SubmitFeedback.SubmitFeedbackValidator;

namespace StackDuel.Application.Tests.Commands.Feedback.SubmitFeedback;

public class SubmitFeedbackValidatorTests
{
    private SubmitFeedbackValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SubmitFeedbackValidator();
    }

    private static SubmitFeedbackCommand ValidCommand(
        int? rating = null,
        FeedbackContextType contextType = FeedbackContextType.None,
        Guid? contextEntityId = null
    ) =>
        new(
            Guid.NewGuid(),
            FeedbackType.General,
            "Something is broken.",
            rating,
            contextType,
            contextEntityId,
            "https://stackduel.dev/problems/two-sum",
            "Mozilla/5.0"
        );

    [Test]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.That(_validator.Validate(ValidCommand()).IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyUserId_IsInvalid()
    {
        var command = ValidCommand() with { UserId = Guid.Empty };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_EmptyMessage_IsInvalid()
    {
        var command = ValidCommand() with { Message = "" };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_MessageTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Message = new string('a', FeedbackMessage.MaxLength + 1) };
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_RatingWithinRange_IsValid([Values(1, 2, 3, 4, 5)] int rating)
    {
        Assert.That(_validator.Validate(ValidCommand(rating: rating)).IsValid, Is.True);
    }

    [Test]
    public void Validate_RatingOutOfRange_IsInvalid([Values(0, 6)] int rating)
    {
        Assert.That(_validator.Validate(ValidCommand(rating: rating)).IsValid, Is.False);
    }

    [Test]
    public void Validate_NullRating_IsValid()
    {
        Assert.That(_validator.Validate(ValidCommand(rating: null)).IsValid, Is.True);
    }

    [Test]
    public void Validate_ContextTypeSetWithoutEntityId_IsInvalid()
    {
        var command = ValidCommand(contextType: FeedbackContextType.Problem, contextEntityId: null);
        Assert.That(_validator.Validate(command).IsValid, Is.False);
    }

    [Test]
    public void Validate_ContextTypeSetWithEntityId_IsValid()
    {
        var command = ValidCommand(contextType: FeedbackContextType.Problem, contextEntityId: Guid.NewGuid());
        Assert.That(_validator.Validate(command).IsValid, Is.True);
    }
}