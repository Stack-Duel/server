using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.Problems.GetProblemReactionSummary;

public sealed record GetProblemReactionSummaryQuery(Guid ProblemId, Guid? UserId) : IQuery<ProblemReactionSummaryDto>;