using StackDuel.Application.Authorization;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Queries.StudioProblems.GetStudioProblemSetups;

public sealed record GetStudioProblemSetupsQuery(Guid ProblemId, Guid RequestedByUserId)
    : IQuery<IEnumerable<StudioProblemDetailsSetupDto>>,
        INotFoundWhenUnauthorized<IEnumerable<StudioProblemDetailsSetupDto>>
{
    public IReadOnlyList<IAuthorizationRequirement> Requirements =>
        [new OwnershipRequirement<Problem>(ProblemId, RequestedByUserId)];
}