using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.ProblemPools.GetProblemPools;

public sealed record GetProblemPoolsQuery : IQuery<IReadOnlyList<ProblemPoolDto>>;