using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Events;
using StackDuel.Domain.Feedback;
using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Feedback.Factories;
using StackDuel.Domain.Feedback.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Commands.Feedback.SubmitFeedback;

internal sealed class SubmitFeedbackHandler(
    IValidator<SubmitFeedbackCommand> validator,
    IAggregateFactory<FeedbackSubmission, CreateFeedbackSubmissionParams> feedbackFactory,
    IFeedbackWriteRepository feedbackRepository,
    IDomainEventDispatcher domainEventDispatcher
) : AbstractCommandHandler<SubmitFeedbackCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        SubmitFeedbackCommand request,
        CancellationToken cancellationToken
    )
    {
        var feedback = feedbackFactory.Create(
            new CreateFeedbackSubmissionParams(
                request.UserId,
                request.Type,
                new FeedbackMessage(request.Message),
                request.Rating is int rating ? new FeedbackRating(rating) : null,
                request.ContextType,
                request.ContextEntityId,
                request.PageUrl,
                request.UserAgent
            )
        );

        await feedbackRepository.AddAsync(feedback, cancellationToken);
        await domainEventDispatcher.DispatchAsync(feedback.PopDomainEvents(), cancellationToken);

        return Result<Guid>.Success(feedback.Id);
    }
}