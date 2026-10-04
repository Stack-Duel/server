using StackDuel.Application.FeatureFlags.Dtos;

namespace StackDuel.Application.Queries.FeatureFlags.GetFeatureFlags;

public sealed record GetFeatureFlagsQuery : IQuery<IReadOnlyList<FeatureFlagAdminDto>>;