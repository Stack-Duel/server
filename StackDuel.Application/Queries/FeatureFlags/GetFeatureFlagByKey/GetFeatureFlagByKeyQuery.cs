using StackDuel.Application.FeatureFlags.Dtos;

namespace StackDuel.Application.Queries.FeatureFlags.GetFeatureFlagByKey;

public sealed record GetFeatureFlagByKeyQuery(string Key) : IQuery<FeatureFlagAdminDto>;