using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Security.Claims;

namespace StackDuel.Api.Tests.Hubs;

internal sealed class HubTestContext
{
    public const string ConnectionId = "connection-1";

    public HubTestContext(string? sub = "auth0|abc")
    {
        Items = new Dictionary<object, object?>();

        ClaimsPrincipal? principal = sub is null
            ? null
            : new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, sub)]));

        Mock<HubCallerContext> callerContext = new();
        callerContext.SetupGet(x => x.ConnectionId).Returns(ConnectionId);
        callerContext.SetupGet(x => x.User).Returns(principal);
        callerContext.SetupGet(x => x.Items).Returns(Items);
        callerContext.SetupGet(x => x.ConnectionAborted).Returns(CancellationToken.None);

        CallerContext = callerContext;
        Groups = new Mock<IGroupManager>();
    }

    public Mock<HubCallerContext> CallerContext { get; }

    public Mock<IGroupManager> Groups { get; }

    public IDictionary<object, object?> Items { get; }

    public void AttachTo(Hub hub)
    {
        hub.Context = CallerContext.Object;
        hub.Groups = Groups.Object;
    }
}