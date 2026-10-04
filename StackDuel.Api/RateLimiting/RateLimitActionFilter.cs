using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.RateLimiting;

/// Reads [RateLimit] off the action or controller and applies
/// the named policy by adding EnableRateLimitingAttribute to
/// the endpoint metadata — exactly what the middleware looks for.
public class RateLimitActionFilter : IAsyncActionFilter, IOrderedFilter
{
    public int Order => int.MinValue; // run before other filters

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // check action first, then controller (action wins)
        var attr = context.ActionDescriptor.EndpointMetadata.OfType<RateLimitAttribute>().FirstOrDefault();

        if (attr is not null)
        {
            // inject the named policy into endpoint metadata
            // so UseRateLimiter picks it up correctly
            context.ActionDescriptor.EndpointMetadata.Add(new EnableRateLimitingAttribute(attr.PolicyName));
        }

        await next();
    }
}