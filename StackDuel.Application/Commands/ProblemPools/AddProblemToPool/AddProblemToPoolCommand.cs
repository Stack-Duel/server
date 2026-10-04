using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.ProblemPools.AddProblemToPool;

internal sealed record AddProblemToPoolCommand(string PoolKey, Guid ProblemId) : ICommand;