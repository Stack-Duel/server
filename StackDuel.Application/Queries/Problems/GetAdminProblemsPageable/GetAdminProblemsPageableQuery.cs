using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetAdminProblemsPageable;

public sealed record GetAdminProblemsPageableQuery(PaginationRequest PaginationRequest, string? Search)
    : IQuery<PageResult<AdminProblemListItemDto>>;