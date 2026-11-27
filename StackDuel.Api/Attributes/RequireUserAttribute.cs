using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StackDuel.Application;

namespace StackDuel.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class RequireUserAttribute : TypeFilterAttribute
{
    public RequireUserAttribute()
        : base(typeof(RequireUserFilter)) { }
}

public sealed class RequireUserFilter(UserContext userContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (userContext.User is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}