using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Api.Requests.Feedback;

public sealed record SubmitFeedbackRequest(
    FeedbackType Type,
    string Message,
    int? Rating,
    FeedbackContextType ContextType,
    Guid? ContextEntityId
);