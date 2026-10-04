namespace StackDuel.Application.Queries.FeatureFlags.GetFeatureFlagEnabled;

public sealed record GetFeatureFlagEnabledQuery(string FlagKey, Guid? UserId) : IQuery<bool>;