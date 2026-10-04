namespace StackDuel.Api.Requests.FeatureFlags;

public sealed record CreateFeatureFlagRequest(string Key, string Name, string Description, bool DefaultEnabled);