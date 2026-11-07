using StackDuel.Application.Authorization;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
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

        Assert.Equal(Ardalis.Result.ResultStatus.NotFound, query.Unauthorized.Status);
    }
}
