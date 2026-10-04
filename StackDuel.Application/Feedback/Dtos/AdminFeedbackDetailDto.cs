using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Feedback.Dtos;

public sealed record AdminFeedbackDetailDto(
    Guid Id,
    FeedbackType Type,
    FeedbackStatus Status,
    string Message,
    int? Rating,
    FeedbackContextType ContextType,
    Guid? ContextEntityId,
    string? AdminNote,
    string? PageUrl,
    string? UserAgent,
    FeedbackUserDto User,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);