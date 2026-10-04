using StackDuel.Application.Feedback.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Queries.Feedback.GetAdminFeedbackPageable;

public sealed record GetAdminFeedbackPageableQuery(
    PaginationRequest PaginationRequest,
    FeedbackType? Type,
    FeedbackStatus? Status
) : IQuery<PageResult<AdminFeedbackListItemDto>>;