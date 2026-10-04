using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Feedback;
using StackDuel.Domain.Feedback.Entities;

namespace StackDuel.Application.Commands.Feedback.UpdateFeedbackStatus;

internal sealed class UpdateFeedbackStatusHandler(
    IValidator<UpdateFeedbackStatusCommand> validator,
    IFeedbackWriteRepository feedbackRepository
) : AbstractCommandHandler<UpdateFeedbackStatusCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateFeedbackStatusCommand request,
        CancellationToken cancellationToken
    )
    {
        FeedbackSubmission? feedback = await feedbackRepository.FindByIdAsync(request.FeedbackId, cancellationToken);

        if (feedback is null)
            return Result.NotFound();

        feedback.UpdateStatus(request.Status, request.AdminNote);
        await feedbackRepository.UpdateAsync(feedback, cancellationToken);

        return Result.Success();
    }
}