using FluentValidation;

namespace StackDuel.Application.Commands.Feedback.UpdateFeedbackStatus;

internal sealed class UpdateFeedbackStatusValidator : AbstractValidator<UpdateFeedbackStatusCommand>
{
    public UpdateFeedbackStatusValidator()
    {
        RuleFor(x => x.FeedbackId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}