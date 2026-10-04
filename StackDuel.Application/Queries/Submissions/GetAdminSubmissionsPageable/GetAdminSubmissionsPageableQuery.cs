using StackDuel.Application.Pagination;
using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Queries.Submissions.GetAdminSubmissionsPageable;

public sealed record GetAdminSubmissionsPageableQuery(PaginationRequest PaginationRequest, Guid? Id)
    : IQuery<PageResult<AdminSubmissionListItemDto>>;