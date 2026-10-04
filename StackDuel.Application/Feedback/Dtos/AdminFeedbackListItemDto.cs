using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Feedback.Dtos;

public sealed record FeedbackUserDto(string Username, string? ImageUrl);

public sealed record AdminFeedbackListItemDto(
    Guid Id,
    FeedbackType Type,
    FeedbackStatus Status,
    string Message,
    int? Rating,
    FeedbackContextType ContextType,
    Guid? ContextEntityId,
    FeedbackUserDto User,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);