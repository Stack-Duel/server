namespace StackDuel.Application.Queries.ProblemPools.GetProblemPoolMemberIds;

public sealed record GetProblemPoolMemberIdsQuery(string PoolKey) : IQuery<IReadOnlyList<Guid>>;