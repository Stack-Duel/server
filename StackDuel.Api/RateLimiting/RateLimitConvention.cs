using StackDuel.Api.Core;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;

namespace StackDuel.Api.RateLimiting;

public enum RateLimitPolicy
{
    Fixed,
    Sliding,
}

/// Custom rate limit attribute.
/// Extend Attribute directly — EnableRateLimitingAttribute is sealed.
/// The policy name is registered at startup and referenced here by name,
/// which is exactly how [EnableRateLimiting("name")] works internally.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RateLimitAttribute : Attribute
{
    public int Limit { get; }
    public string Window { get; }
    public RateLimitPolicy Policy { get; init; } = RateLimitPolicy.Fixed;
    public int QueueLimit { get; init; } = 5;

    // the policy name this attribute maps to in the rate limiter registry
    public string PolicyName { get; }

    public RateLimitAttribute(int limit, string window)
    {
        Limit = limit;
        Window = window;
        PolicyName = BuildPolicyName(limit, window, Policy);
    }

    // deterministic name — same args always produce the same name
    // e.g. [RateLimit(100, "1m")]         → "rl:fixed:100:60"
    //      [RateLimit(5, "30s", Sliding)] → "rl:sliding:5:30"
    internal static string BuildPolicyName(int limit, string window, RateLimitPolicy policy = RateLimitPolicy.Fixed)
    {
        int seconds = TimeStringParser.ToSeconds(window);
        return $"rl:{policy.ToString().ToLower()}:{limit}:{seconds}";
    }
}

/// Convention that automatically adds RateLimitActionFilter to all controllers.
/// This ensures [RateLimit] attributes are discovered and applied at startup.
public class RateLimitConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        action.Filters.Add(new RateLimitActionFilter());
    }
}