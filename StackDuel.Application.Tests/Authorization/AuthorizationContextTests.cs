using StackDuel.Application.Authorization;

namespace StackDuel.Application.Tests.Authorization;

public class AuthorizationContextTests
{
    private sealed record FakeRequirement : IAuthorizationRequirement;

    [Fact]
    public void AllSucceeded_NoRequirements_IsTrue()
    {
        var context = new AuthorizationContext([], CancellationToken.None);

        Assert.True(context.AllSucceeded);
    }

    [Fact]
    public void AllSucceeded_RequirementNotSucceeded_IsFalse()
    {
        var requirement = new FakeRequirement();
        var context = new AuthorizationContext([requirement], CancellationToken.None);

        Assert.False(context.AllSucceeded);
    }

    [Fact]
    public void AllSucceeded_AllRequirementsSucceeded_IsTrue()
    {
        var first = new FakeRequirement();
        var second = new FakeRequirement();
        var context = new AuthorizationContext([first, second], CancellationToken.None);

        context.Succeed(first);
        context.Succeed(second);

        Assert.True(context.AllSucceeded);
    }

    [Fact]
    public void AllSucceeded_OnlySomeRequirementsSucceeded_IsFalse()
    {
        var first = new FakeRequirement();
        var second = new FakeRequirement();
        var context = new AuthorizationContext([first, second], CancellationToken.None);

        context.Succeed(first);

        Assert.False(context.AllSucceeded);
    }
}