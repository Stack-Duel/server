using StackDuel.Application.Pagination;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.ProblemPools.GetProblemPoolMembersPaged;

public sealed record GetProblemPoolMembersPagedQuery(string PoolKey, PaginationRequest PaginationRequest)
    : IQuery<PageResult<AdminProblemListItemDto>>;