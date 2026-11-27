using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using StackDuel.Api.Attributes;
using StackDuel.Api.Tests.Controllers;

namespace StackDuel.Api.Tests.Attributes;

public class RequireUserFilterTests
{
    private static ActionExecutingContext EmptyActionContext() =>
        new(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            [],
            new Dictionary<string, object?>(),
            controller: null
        );

    [Fact]
    public async Task AnonymousRequest_IsUnauthorizedAndDoesNotRunTheAction()
    {
        RequireUserFilter filter = new(ControllerAssert.AnonymousContext());
        ActionExecutingContext context = EmptyActionContext();
        bool actionRan = false;

        await filter.OnActionExecutionAsync(
            context,
            () =>
            {
                actionRan = true;
                return Task.FromResult<ActionExecutedContext>(null!);
            }
        );

        Assert.IsType<UnauthorizedResult>(context.Result);
        Assert.False(actionRan);
    }

    [Fact]
    public async Task AuthenticatedRequest_RunsTheAction()
    {
        RequireUserFilter filter = new(ControllerAssert.AuthenticatedContext());
        ActionExecutingContext context = EmptyActionContext();
        bool actionRan = false;

        await filter.OnActionExecutionAsync(
            context,
            () =>
            {
                actionRan = true;
                return Task.FromResult<ActionExecutedContext>(null!);
            }
        );

        Assert.Null(context.Result);
        Assert.True(actionRan);
    }
}