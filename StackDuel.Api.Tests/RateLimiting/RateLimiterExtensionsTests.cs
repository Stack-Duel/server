using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackDuel.Api.RateLimiting;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace StackDuel.Api.Tests.RateLimiting;

public class RateLimiterExtensionsTests
{
    private readonly RateLimiterOptions _options;

    public RateLimiterExtensionsTests()
    {
        ServiceCollection services = new();
        services.AddAttributeRateLimiting();

        _options = services.BuildServiceProvider().GetRequiredService<IOptions<RateLimiterOptions>>().Value;
    }

    private static HttpContext ContextFor(string? userId = null, string? policyName = null, string? ip = null)
    {
        DefaultHttpContext context = new();

        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test")
            );
        }

        if (ip is not null)
            context.Connection.RemoteIpAddress = IPAddress.Parse(ip);

        if (policyName is not null)
        {
            context.SetEndpoint(
                new Endpoint(
                    _ => Task.CompletedTask,
                    new EndpointMetadataCollection(new EnableRateLimitingAttribute(policyName)),
                    "test"
                )
            );
        }

        return context;
    }

    private async Task<int> CountAllowedAsync(HttpContext context, int attempts)
    {
        int allowed = 0;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            using RateLimitLease lease = await _options.GlobalLimiter!.AcquireAsync(context, 1, CancellationToken.None);
            if (lease.IsAcquired)
                allowed++;
        }

        return allowed;
    }

    [Fact]
    public void TheRejectionStatus_IsTooManyRequests()
    {
        Assert.Equal(StatusCodes.Status429TooManyRequests, _options.RejectionStatusCode);
        Assert.NotNull(_options.GlobalLimiter);
        Assert.NotNull(_options.OnRejected);
    }

    [Fact]
    public async Task AnEndpointWithNoPolicy_IsOnlyHeldToTheGlobalCeiling()
    {
        int allowed = await CountAllowedAsync(ContextFor(userId: "no-policy-user"), 40);

        Assert.Equal(40, allowed);
    }

    [Fact]
    public async Task TheSustainedSubmissionTier_StopsAUserWellShortOfTheGlobalCeiling()
    {
        int allowed = await CountAllowedAsync(
            ContextFor(userId: "run-user", policyName: WellKnownPolicies.Submissions),
            40
        );

        Assert.Equal(30, allowed);
    }

    [Fact]
    public async Task TheSustainedGradedTier_IsTighterThanThePlainSubmissionTier()
    {
        int allowed = await CountAllowedAsync(
            ContextFor(userId: "grade-user", policyName: WellKnownPolicies.GradedSubmissions),
            40
        );

        Assert.Equal(15, allowed);
    }

    [Fact]
    public async Task TheRunAndGradeTiers_AreCountedSeparatelyForTheSameUser()
    {
        await CountAllowedAsync(ContextFor(userId: "both-user", policyName: WellKnownPolicies.Submissions), 30);

        int gradedAllowed = await CountAllowedAsync(
            ContextFor(userId: "both-user", policyName: WellKnownPolicies.GradedSubmissions),
            15
        );

        Assert.Equal(15, gradedAllowed);
    }

    [Fact]
    public async Task OneUsersSubmissions_DoNotEatAnothersAllowance()
    {
        await CountAllowedAsync(ContextFor(userId: "noisy", policyName: WellKnownPolicies.Submissions), 40);

        int quietAllowed = await CountAllowedAsync(
            ContextFor(userId: "quiet", policyName: WellKnownPolicies.Submissions),
            10
        );

        Assert.Equal(10, quietAllowed);
    }

    [Fact]
    public async Task AnonymousCallers_ArePartitionedByAddressRatherThanLumpedTogether()
    {
        await CountAllowedAsync(ContextFor(policyName: WellKnownPolicies.Submissions, ip: "203.0.113.1"), 40);

        int otherAddressAllowed = await CountAllowedAsync(
            ContextFor(policyName: WellKnownPolicies.Submissions, ip: "203.0.113.2"),
            10
        );

        Assert.Equal(10, otherAddressAllowed);
    }

    [Fact]
    public async Task ACallerWithNeitherAClaimNorAnAddress_IsStillLimited()
    {
        int allowed = await CountAllowedAsync(ContextFor(policyName: WellKnownPolicies.Submissions), 40);

        Assert.Equal(30, allowed);
    }

    [Fact]
    public async Task ARejection_ExplainsTheWaitInTheBodyAndTheRetryAfterHeader()
    {
        using TokenBucketRateLimiter limiter = new(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromSeconds(7),
                AutoReplenishment = false,
                QueueLimit = 0,
            }
        );

        using RateLimitLease granted = limiter.AttemptAcquire(1);
        using RateLimitLease rejected = limiter.AttemptAcquire(1);

        Assert.True(granted.IsAcquired);
        Assert.False(rejected.IsAcquired);

        HttpContext context = new DefaultHttpContext();
        MemoryStream body = new();
        context.Response.Body = body;

        await _options.OnRejected!(new OnRejectedContext { HttpContext = context, Lease = rejected }, default);

        Assert.Equal(429, context.Response.StatusCode);
        Assert.Equal("7", context.Response.Headers.RetryAfter);

        body.Position = 0;
        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(7, payload.GetProperty("retryAfterSeconds").GetInt32());
        Assert.Contains("too quickly", payload.GetProperty("message").GetString()!);
        Assert.Contains("7 second(s)", payload.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task ARejectionWithNoRetryHint_FallsBackToAShortDefaultWait()
    {
        using ConcurrencyLimiter limiter = new(
            new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 }
        );

        using RateLimitLease granted = limiter.AttemptAcquire(1);
        using RateLimitLease rejected = limiter.AttemptAcquire(1);

        Assert.False(rejected.IsAcquired);
        Assert.False(rejected.TryGetMetadata(MetadataName.RetryAfter, out _));

        HttpContext context = new DefaultHttpContext();
        MemoryStream body = new();
        context.Response.Body = body;

        await _options.OnRejected!(new OnRejectedContext { HttpContext = context, Lease = rejected }, default);

        Assert.Equal("5", context.Response.Headers.RetryAfter);

        body.Position = 0;
        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(5, payload.GetProperty("retryAfterSeconds").GetInt32());
    }

    [Fact]
    public async Task ASubSecondRetryHint_IsRoundedUpToAWholeSecond()
    {
        using TokenBucketRateLimiter limiter = new(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromMilliseconds(200),
                AutoReplenishment = false,
                QueueLimit = 0,
            }
        );

        using RateLimitLease granted = limiter.AttemptAcquire(1);
        using RateLimitLease rejected = limiter.AttemptAcquire(1);

        HttpContext context = new DefaultHttpContext();
        MemoryStream body = new();
        context.Response.Body = body;

        await _options.OnRejected!(new OnRejectedContext { HttpContext = context, Lease = rejected }, default);

        Assert.Equal("1", context.Response.Headers.RetryAfter);
    }
}