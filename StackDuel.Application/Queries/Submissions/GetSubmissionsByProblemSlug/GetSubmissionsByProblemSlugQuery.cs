using StackDuel.Application.Pagination;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Queries.Submissions.GetSubmissionsByProblemSlug;

internal sealed record GetSubmissionsByProblemSlugQuery(
    string ProblemSlug,
    PaginationRequest PaginationRequest,
    Guid? UserId,
    SubmissionFilter Type,
    SubmissionSortOrder SortOrder
) : IQuery<PageResult<ProblemSubmissionDto>>;