using StackDuel.Application.Pagination;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Submissions.GetAdminSubmissionsPageable;

internal sealed class GetAdminSubmissionsPageableHandler(ISubmissionReadRepository submissionReadRepository)
    : IQueryHandler<GetAdminSubmissionsPageableQuery, PageResult<AdminSubmissionListItemDto>>
{
    public async Task<Result<PageResult<AdminSubmissionListItemDto>>> Handle(
        GetAdminSubmissionsPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await submissionReadRepository.GetAdminSubmissionsPagedAsync(
            request.PaginationRequest,
            request.Id,
            cancellationToken
        );

        return Result.Success(result);
    }
}