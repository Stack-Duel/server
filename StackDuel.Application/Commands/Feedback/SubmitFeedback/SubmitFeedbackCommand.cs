using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Commands.Feedback.SubmitFeedback;

internal sealed record SubmitFeedbackCommand(
    Guid UserId,
    FeedbackType Type,
    string Message,
    int? Rating,
    FeedbackContextType ContextType,
    Guid? ContextEntityId,
    string? PageUrl,
    string? UserAgent
) : ICommand<Guid>;