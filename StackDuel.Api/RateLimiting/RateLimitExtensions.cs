using StackDuel.Api.Core;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;
using System.Threading.RateLimiting;

namespace StackDuel.Api.RateLimiting;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddAttributeRateLimiting(this IServiceCollection services)
    {
        IEnumerable<RateLimitAttribute> attributes = DiscoverAttributes();

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                string key =
                    ctx.User?.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    key,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                    }
                );
            });

            options.AddPolicy(
                WellKnownPolicies.General,
                ctx =>
                {
                    string key =
                        ctx.User?.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 300,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10,
                        }
                    );
                }
            );

            options.AddPolicy(
                WellKnownPolicies.Submissions,
                ctx =>
                {
                    string key =
                        ctx.User?.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        key,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2,
                        }
                    );
                }
            );

            options.AddPolicy(
                WellKnownPolicies.AvatarUpload,
                ctx =>
                {
                    string key =
                        ctx.User?.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        key,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(10),
                            SegmentsPerWindow = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 1,
                        }
                    );
                }
            );

            options.AddFixedWindowLimiter(
                WellKnownPolicies.Judge0Callback,
                opt =>
                {
                    opt.PermitLimit = 600;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                }
            );

            foreach (RateLimitAttribute attr in attributes)
                RegisterPolicy(options, attr);

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = 429;
                ctx.HttpContext.Response.ContentType = "application/json";

                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await ctx.HttpContext.Response.WriteAsync(
                    """{"error":"Too many requests. Please try again later."}""",
                    token
                );
            };
        });

        services.AddControllers(options => options.Conventions.Add(new RateLimitConvention()));

        return services;
    }

    private static IEnumerable<RateLimitAttribute> DiscoverAttributes() =>
        Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .SelectMany(type =>
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Select(m => m.GetCustomAttribute<RateLimitAttribute>())
                    .Append(type.GetCustomAttribute<RateLimitAttribute>())
                    .Where(a => a is not null)
                    .Select(a => a!)
            )
            .DistinctBy(a => a.PolicyName)
            .ToList();

    private static void RegisterPolicy(RateLimiterOptions options, RateLimitAttribute attr)
    {
        TimeSpan window = TimeStringParser.ToTimeSpan(attr.Window);

        switch (attr.Policy)
        {
            case RateLimitPolicy.Fixed:
                options.AddFixedWindowLimiter(
                    attr.PolicyName,
                    opt =>
                    {
                        opt.PermitLimit = attr.Limit;
                        opt.Window = window;
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = attr.QueueLimit;
                    }
                );
                break;

            case RateLimitPolicy.Sliding:
                options.AddSlidingWindowLimiter(
                    attr.PolicyName,
                    opt =>
                    {
                        opt.PermitLimit = attr.Limit;
                        opt.Window = window;
                        opt.SegmentsPerWindow = CalculateSegments(window);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = attr.QueueLimit;
                    }
                );
                break;
        }
    }

    private static int CalculateSegments(TimeSpan window) =>
        window.TotalSeconds switch
        {
            <= 60 => 6,
            <= 300 => 5,
            <= 3600 => 6,
            _ => 4,
        };
}