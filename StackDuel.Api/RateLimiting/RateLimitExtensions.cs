using Microsoft.AspNetCore.RateLimiting;
using StackDuel.Api.Core;
using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace StackDuel.Api.RateLimiting;

public static class RateLimiterExtensions
{
    private const int DefaultRetryAfterSeconds = 5;

    private static string PartitionKeyFor(HttpContext context) =>
        context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    private static string? PolicyNameFor(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

    public static IServiceCollection AddAttributeRateLimiting(this IServiceCollection services)
    {
        IEnumerable<RateLimitAttribute> attributes = DiscoverAttributes();

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        PartitionKeyFor(ctx),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 300,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10,
                        }
                    )
                ),
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    PolicyNameFor(ctx) switch
                    {
                        WellKnownPolicies.Submissions => RateLimitPartition.GetSlidingWindowLimiter(
                            $"sustained:run:{PartitionKeyFor(ctx)}",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 30,
                                Window = TimeSpan.FromMinutes(5),
                                SegmentsPerWindow = 5,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0,
                            }
                        ),
                        WellKnownPolicies.GradedSubmissions => RateLimitPartition.GetSlidingWindowLimiter(
                            $"sustained:grade:{PartitionKeyFor(ctx)}",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 15,
                                Window = TimeSpan.FromMinutes(10),
                                SegmentsPerWindow = 5,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0,
                            }
                        ),
                        _ => RateLimitPartition.GetNoLimiter<string>("unlimited"),
                    }
                )
            );

            options.AddPolicy(
                WellKnownPolicies.General,
                ctx =>
                {
                    string key =
                        ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? ctx.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

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
                    RateLimitPartition.GetTokenBucketLimiter(
                        $"burst:run:{PartitionKeyFor(ctx)}",
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 3,
                            TokensPerPeriod = 1,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(3),
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        }
                    )
            );

            options.AddPolicy(
                WellKnownPolicies.GradedSubmissions,
                ctx =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        $"burst:grade:{PartitionKeyFor(ctx)}",
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 2,
                            TokensPerPeriod = 1,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                        }
                    )
            );

            options.AddPolicy(
                WellKnownPolicies.AvatarUpload,
                ctx =>
                {
                    string key =
                        ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? ctx.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

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

            options.AddPolicy(
                WellKnownPolicies.ProblemCreation,
                ctx =>
                {
                    string key =
                        ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? ctx.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        key,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 15,
                            Window = TimeSpan.FromHours(1),
                            SegmentsPerWindow = 6,
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

                int retryAfterSeconds = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter)
                    ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                    : DefaultRetryAfterSeconds;

                ctx.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                await ctx.HttpContext.Response.WriteAsJsonAsync(
                    new RateLimitRejection(
                        $"You are sending requests too quickly. Please wait {retryAfterSeconds} second(s) before trying again.",
                        retryAfterSeconds
                    ),
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

internal sealed record RateLimitRejection(string Message, int RetryAfterSeconds);