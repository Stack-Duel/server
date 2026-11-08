using StackDuel.Application.Authorization;

namespace StackDuel.Application.Tests.Authorization;

public class PermissionRequirementHandlerTests
{
    [Fact]
    public async Task HandleAsync_UserHasPermission_SucceedsTheRequirement()
    {
        var userContext = new UserContext { Permissions = ["problem:delete"] };
        var handler = new PermissionRequirementHandler(userContext);
        var requirement = new PermissionRequirement("problem:delete");

        var context = new AuthorizationContext([requirement], CancellationToken.None);

        await handler.HandleAsync(context);

        Assert.True(context.AllSucceeded);
    }

    [Fact]
    public async Task HandleAsync_UserMissingPermission_DoesNotSucceedTheRequirement()
    {
        var userContext = new UserContext { Permissions = [] };
        var handler = new PermissionRequirementHandler(userContext);
        var requirement = new PermissionRequirement("problem:delete");

        var context = new AuthorizationContext([requirement], CancellationToken.None);

        await handler.HandleAsync(context);

        Assert.False(context.AllSucceeded);
    }
}