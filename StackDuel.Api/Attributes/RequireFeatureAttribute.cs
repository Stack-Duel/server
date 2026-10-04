using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StackDuel.Application;

namespace StackDuel.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class RequireFeatureAttribute : TypeFilterAttribute
{
    public RequireFeatureAttribute(string featureKey)
        : base(typeof(RequireFeatureFilter))
    {
        if (string.IsNullOrWhiteSpace(featureKey))
            throw new ArgumentException("Feature key is required.", nameof(featureKey));

        Arguments = [featureKey];
    }
}

public sealed class RequireFeatureFilter(UserContext userContext, string featureKey) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!userContext.HasFeature(featureKey))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}