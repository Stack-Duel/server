using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.ProblemPools.GetOrderedProblemPoolMembers;

public sealed record GetOrderedProblemPoolMembersQuery(string PoolKey)
    : IQuery<IReadOnlyList<AdminProblemListRowDto>>;