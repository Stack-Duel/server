using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Feedback.Dtos;

public sealed record SubmitFeedbackDto(
    Guid UserId,
    FeedbackType Type,
    string Message,
    int? Rating,
    FeedbackContextType ContextType,
    Guid? ContextEntityId,
    string? PageUrl,
    string? UserAgent
);