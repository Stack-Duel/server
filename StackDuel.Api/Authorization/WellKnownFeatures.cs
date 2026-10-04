using StackDuel.Domain.FeatureFlags;

namespace StackDuel.Api.Authorization;

public static class WellKnownFeatures
{
    public const string Leaderboards = WellKnownFeatureFlags.Leaderboards;
    public const string Campaigns = WellKnownFeatureFlags.Campaigns;
}