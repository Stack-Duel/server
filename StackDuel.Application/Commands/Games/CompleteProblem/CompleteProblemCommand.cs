using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Games.CompleteProblem;

public sealed record CompleteProblemResultDto(int NewScore, Guid? NextProblemId);

internal sealed record CompleteProblemCommand(Guid GameId, Guid ProblemId, Guid SubmissionId, Guid RequestedByUserId)
    : ICommand<CompleteProblemResultDto>;