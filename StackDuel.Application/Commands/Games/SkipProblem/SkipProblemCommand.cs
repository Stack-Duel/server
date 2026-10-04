using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Games.SkipProblem;

public sealed record SkipProblemResultDto(int SkipsRemaining, Guid? NextProblemId);

internal sealed record SkipProblemCommand(Guid GameId, Guid ProblemId, Guid RequestedByUserId)
    : ICommand<SkipProblemResultDto>;