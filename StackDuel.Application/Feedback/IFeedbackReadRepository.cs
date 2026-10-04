using StackDuel.Application.Feedback.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Feedback;

public interface IFeedbackReadRepository
{
    Task<PageResult<AdminFeedbackListItemDto>> GetAdminFeedbackPagedAsync(
        PaginationRequest paginationRequest,
        FeedbackType? type,
        FeedbackStatus? status,
        CancellationToken cancellationToken = default
    );

    Task<AdminFeedbackDetailDto?> FindAdminFeedbackByIdAsync(
        Guid feedbackId,
        CancellationToken cancellationToken = default
    );
}