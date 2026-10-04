using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.Feedback.ValueObjects;
using FluentValidation;

namespace StackDuel.Application.Commands.Feedback.SubmitFeedback;

internal sealed class SubmitFeedbackValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(FeedbackMessage.MaxLength);

        RuleFor(x => x.Rating)
            .InclusiveBetween(FeedbackRating.MinValue, FeedbackRating.MaxValue)
            .When(x => x.Rating.HasValue);

        RuleFor(x => x.ContextEntityId)
            .NotEmpty()
            .When(x => x.ContextType != FeedbackContextType.None)
            .WithMessage("Context entity id is required when a context type is set.");
    }
}