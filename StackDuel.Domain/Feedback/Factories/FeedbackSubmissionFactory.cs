using StackDuel.Domain.Feedback.Entities;
using StackDuel.Domain.Feedback.Enums;
using StackDuel.Domain.Feedback.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Feedback.Factories;

public sealed record CreateFeedbackSubmissionParams(
    Guid UserId,
    FeedbackType Type,
    FeedbackMessage Message,
    FeedbackRating? Rating,
    FeedbackContextType ContextType,
    Guid? ContextEntityId,
    string? PageUrl,
    string? UserAgent
);

public sealed class FeedbackSubmissionFactory : IAggregateFactory<FeedbackSubmission, CreateFeedbackSubmissionParams>
{
    public FeedbackSubmission Create(CreateFeedbackSubmissionParams parameters)
    {
        return new FeedbackSubmission(
            parameters.UserId,
            parameters.Type,
            parameters.Message,
            parameters.Rating,
            parameters.ContextType,
            parameters.ContextEntityId,
            parameters.PageUrl,
            parameters.UserAgent
        );
    }
}