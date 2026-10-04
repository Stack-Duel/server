using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.ProblemPools.ReorderProblemPool;

internal sealed record ReorderProblemPoolCommand(string PoolKey, IReadOnlyList<Guid> ProblemIds) : ICommand;