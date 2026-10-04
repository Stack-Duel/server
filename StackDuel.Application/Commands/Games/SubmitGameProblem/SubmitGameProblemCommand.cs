using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Games.SubmitGameProblem;

internal sealed record SubmitGameProblemCommand(
    Guid GameId,
    Guid ProblemId,
    Guid ProblemSetupId,
    string Code,
    Guid RequestedByUserId
) : ICommand<Guid>;