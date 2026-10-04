using Microsoft.ApplicationInsights.DataContracts;
using StackDuel.Api.Attributes;
using StackDuel.Application;
using StackDuel.Application.Services.FeatureFlags;
using StackDuel.Application.Services.Users;
using System.Security.Claims;

namespace StackDuel.Api.Middleware;

public partial class AccountContextMiddleware(
    IUserService userService,
    IFeatureFlagResolutionService featureFlagResolutionService,
    UserContext userContext,
    ILogger<AccountContextMiddleware> logger
) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();
        bool requiresUser = endpoint?.Metadata.GetMetadata<RequireUserAttribute>() != null;

        bool isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        string? sub = isAuthenticated ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;

        Guid? userId = null;

        if (requiresUser && !string.IsNullOrEmpty(sub))
        {
            var result = await userService.GetAccessContextBySubAsync(sub, context.RequestAborted);
            if (result.IsSuccess)
            {
                userContext.User = result.Value.User;
                userContext.Permissions = result.Value.Permissions;
                userContext.Roles = result.Value.Roles;
                userId = result.Value.User.Id;

                var requestTelemetry = context.Features.Get<RequestTelemetry>();
                if (requestTelemetry is not null)
                {
                    requestTelemetry.Properties.TryAdd("account.id", userContext.User.Id.ToString());
                    requestTelemetry.Properties.TryAdd("account.username", userContext.User.Username);
                    requestTelemetry.Properties.TryAdd("account.roles", string.Join(", ", userContext.Roles));
                }
            }
            else
            {
                LogResolveFailed(sub, context.Request.Path, string.Join(", ", result.Errors));
            }
        }
        else if (requiresUser)
        {
            LogMissingSub(context.Request.Path);
        }
        else
        {
            LogPublicEndpoint(context.Request.Path);

            if (!string.IsNullOrEmpty(sub))
            {
                var result = await userService.GetAccessContextBySubAsync(sub, context.RequestAborted);
                if (result.IsSuccess)
                {
                    userContext.User = result.Value.User;
                    userContext.Permissions = result.Value.Permissions;
                    userContext.Roles = result.Value.Roles;
                    userId = result.Value.User.Id;
                }
            }
        }

        userContext.Flags = await featureFlagResolutionService.GetFlagsForViewerAsync(userId, context.RequestAborted);

        await next(context);
    }

    [LoggerMessage(
        EventId = LoggingEventIds.Accounts.ContextMissingSub,
        Level = LogLevel.Warning,
        Message = "Account context: missing sub claim on [RequiresUser] endpoint {path}"
    )]
    private partial void LogMissingSub(string path);

    [LoggerMessage(
        EventId = LoggingEventIds.Accounts.ContextResolveFailed,
        Level = LogLevel.Warning,
        Message = "Account context: failed to resolve account for sub {sub} on {path}: {errors}"
    )]
    private partial void LogResolveFailed(string sub, string path, string errors);

    [LoggerMessage(
        EventId = LoggingEventIds.Accounts.ContextPublicEndpoint,
        Level = LogLevel.Debug,
        Message = "Account context: public endpoint {path}, skipping user resolution"
    )]
    private partial void LogPublicEndpoint(string path);
}