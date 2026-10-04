using Ardalis.Result;
using StackDuel.Application.Feedback;
using StackDuel.Application.Feedback.Dtos;
using StackDuel.Application.Pagination;

namespace StackDuel.Application.Queries.Feedback.GetAdminFeedbackPageable;

internal sealed class GetAdminFeedbackPageableHandler(IFeedbackReadRepository feedbackReadRepository)
    : IQueryHandler<GetAdminFeedbackPageableQuery, PageResult<AdminFeedbackListItemDto>>
{
    public async Task<Result<PageResult<AdminFeedbackListItemDto>>> Handle(
        GetAdminFeedbackPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await feedbackReadRepository.GetAdminFeedbackPagedAsync(
            request.PaginationRequest,
            request.Type,
            request.Status,
            cancellationToken
        );

        return Result.Success(result);
    }
}