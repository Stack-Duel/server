using MediatR;
using StackDuel.Application.Authorization;
using StackDuel.Application.Behaviors;

namespace StackDuel.Application.Tests.Behavior;

public class AuthorizationBehaviorTests
{
    private sealed record FakeRequirement : IAuthorizationRequirement;

    private sealed class FakeHandler(bool succeeds) : AuthorizationHandler<FakeRequirement>
    {
        protected override Task<bool> IsSatisfiedAsync(FakeRequirement requirement, CancellationToken cancellationToken) =>
            Task.FromResult(succeeds);
    }

    private sealed record GuardedRequest : IRequest<string>, IAuthorizationGuarded<string>
    {
        public IReadOnlyList<IAuthorizationRequirement> Requirements => [new FakeRequirement()];

        public string Unauthorized => "denied";
    }

    private sealed record PlainRequest : IRequest<string>;

    [Fact]
    public async Task Handle_AllRequirementsSatisfied_CallsNext()
    {
        var behavior = new AuthorizationBehavior<GuardedRequest, string>([new FakeHandler(succeeds: true)]);

        string result = await behavior.Handle(new GuardedRequest(), _ => Task.FromResult("handled"), CancellationToken.None);

        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Handle_RequirementNotSatisfied_ShortCircuitsWithUnauthorizedResponse()
    {
        var behavior = new AuthorizationBehavior<GuardedRequest, string>([new FakeHandler(succeeds: false)]);
        bool nextCalled = false;

        string result = await behavior.Handle(
            new GuardedRequest(),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("handled");
            },
            CancellationToken.None
        );

        Assert.Equal("denied", result);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_RequestNotGuarded_CallsNextWithoutRunningHandlers()
    {
        var behavior = new AuthorizationBehavior<PlainRequest, string>([new FakeHandler(succeeds: false)]);

        string result = await behavior.Handle(new PlainRequest(), _ => Task.FromResult("handled"), CancellationToken.None);

        Assert.Equal("handled", result);
    }
}