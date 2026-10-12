using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.Feedback.ValueObjects;
using SubmitFeedbackCommand = StackDuel.Application.Commands.Feedback.SubmitFeedback.SubmitFeedbackCommand;
using SubmitFeedbackValidator = StackDuel.Application.Commands.Feedback.SubmitFeedback.SubmitFeedbackValidator;

namespace StackDuel.Application.Tests.Commands.Feedback.SubmitFeedback;

public class SubmitFeedbackValidatorTests
{
    private SubmitFeedbackValidator _validator = null!;

    public SubmitFeedbackValidatorTests()
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

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Validate_EmptyUserId_IsInvalid()
    {
        var command = ValidCommand() with { UserId = Guid.Empty };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyMessage_IsInvalid()
    {
        var command = ValidCommand() with { Message = "" };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_MessageTooLong_IsInvalid()
    {
        var command = ValidCommand() with { Message = new string('a', FeedbackMessage.MaxLength + 1) };
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Validate_RatingWithinRange_IsValid(int rating)
    {
        Assert.True(_validator.Validate(ValidCommand(rating: rating)).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_RatingOutOfRange_IsInvalid(int rating)
    {
        Assert.False(_validator.Validate(ValidCommand(rating: rating)).IsValid);
    }

    [Fact]
    public void Validate_NullRating_IsValid()
    {
        Assert.True(_validator.Validate(ValidCommand(rating: null)).IsValid);
    }

    [Fact]
    public void Validate_ContextTypeSetWithoutEntityId_IsInvalid()
    {
        var command = ValidCommand(contextType: FeedbackContextType.Problem, contextEntityId: null);
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_ContextTypeSetWithEntityId_IsValid()
    {
        var command = ValidCommand(contextType: FeedbackContextType.Problem, contextEntityId: Guid.NewGuid());
        Assert.True(_validator.Validate(command).IsValid);
    }
}