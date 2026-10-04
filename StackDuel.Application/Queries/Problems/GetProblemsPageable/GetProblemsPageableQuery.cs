using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetProblemsPageable;

public sealed record GetProblemsPageableQuery(PaginationRequest PaginationRequest, string? Search)
    : IQuery<PageResult<ProblemDto>>;