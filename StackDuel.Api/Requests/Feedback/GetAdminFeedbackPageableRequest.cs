using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Api.Requests.Feedback;

public sealed record GetAdminFeedbackPageableRequest(
    int Page,
    int Size,
    DateTime Timestamp,
    FeedbackType? Type,
    FeedbackStatus? Status
);