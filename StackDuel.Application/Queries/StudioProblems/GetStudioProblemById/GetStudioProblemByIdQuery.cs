using Ardalis.Result;
using StackDuel.Application.Authorization;
using StackDuel.Application.Queries;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;

public sealed record GetStudioProblemByIdQuery(Guid ProblemId, Guid RequestedByUserId)
    : IQuery<StudioProblemDetailsDto>, IAuthorizationGuarded<Result<StudioProblemDetailsDto>>
{
    public IReadOnlyList<IAuthorizationRequirement> Requirements =>
        [new OwnershipRequirement<Problem>(ProblemId, RequestedByUserId)];

    public Result<StudioProblemDetailsDto> Unauthorized => Result<StudioProblemDetailsDto>.NotFound();
}
