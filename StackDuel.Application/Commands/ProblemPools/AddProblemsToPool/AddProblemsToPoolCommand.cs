using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.ProblemPools.AddProblemsToPool;

internal sealed record AddProblemsToPoolCommand(
    string PoolKey,
    IReadOnlyList<Guid> ProblemIds,
    bool SelectAllMatching,
    string? Search,
    IReadOnlyList<Guid> ExcludedProblemIds
) : ICommand<int>;