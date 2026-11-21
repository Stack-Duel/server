using StackDuel.Application.Authorization;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.StudioProblems.GetStudioProblemDeletionImpact;

public sealed record GetStudioProblemDeletionImpactQuery(Guid ProblemId, Guid RequestedByUserId)
    : IQuery<ProblemDeletionImpactDto>;