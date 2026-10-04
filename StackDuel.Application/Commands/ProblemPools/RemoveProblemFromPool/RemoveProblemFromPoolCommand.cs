using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.ProblemPools.RemoveProblemFromPool;

internal sealed record RemoveProblemFromPoolCommand(string PoolKey, Guid ProblemId) : ICommand;