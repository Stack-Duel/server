using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.ProblemPools.CreateProblemPool;

internal sealed record CreateProblemPoolCommand(string Key, string Name, string? Description) : ICommand<Guid>;