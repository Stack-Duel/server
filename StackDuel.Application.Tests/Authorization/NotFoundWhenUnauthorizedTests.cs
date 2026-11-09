using Ardalis.Result;
using StackDuel.Application.Authorization;

namespace StackDuel.Application.Tests.Authorization;

public class NotFoundWhenUnauthorizedTests
{
    private sealed record GuardedRequest : INotFoundWhenUnauthorized<string>
    {
        public IReadOnlyList<IAuthorizationRequirement> Requirements => [];
    }

    [Fact]
    public void Unauthorized_ReturnsNotFoundResult()
    {
        var request = new GuardedRequest();

        IAuthorizationGuarded<Result<string>> guarded = request;

        Assert.Equal(ResultStatus.NotFound, guarded.Unauthorized.Status);
    }
}