using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Commands.Problems.SetProblemReaction;

internal sealed record SetProblemReactionCommand(Guid ProblemId, Guid UserId, string ReactionTypeKey)
    : ICommand<ProblemReactionSummaryDto>;