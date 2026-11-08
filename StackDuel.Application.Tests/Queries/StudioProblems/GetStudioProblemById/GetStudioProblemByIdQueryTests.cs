using Ardalis.Result;
using StackDuel.Application.Authorization;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Tests.Queries.StudioProblems.GetStudioProblemById;

public class GetStudioProblemByIdQueryTests
{
    [Fact]
    public void Requirements_DeclaresOwnershipOfTheProblem()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var query = new GetStudioProblemByIdQuery(problemId, userId);

        IAuthorizationRequirement requirement = Assert.Single(query.Requirements);

        var ownership = Assert.IsType<OwnershipRequirement<Problem>>(requirement);
        Assert.Equal(problemId, ownership.ResourceId);
        Assert.Equal(userId, ownership.UserId);
    }

    [Fact]
    public void Unauthorized_IsNotFound()
    {
        var query = new GetStudioProblemByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        IAuthorizationGuarded<Result<StudioProblemDetailsDto>> guarded = query;

        Assert.Equal(ResultStatus.NotFound, guarded.Unauthorized.Status);
    }
}