using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Application.FeatureFlags.Dtos;

public sealed record FeatureFlagAdminDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    bool DefaultEnabled,
    int RolloutPercentage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FeatureFlagUserOverrideDto> UserOverrides,
    IReadOnlyList<FeatureFlagGroupOverrideDto> GroupOverrides
);

public sealed record FeatureFlagUserOverrideDto(Guid UserId, DecisionEffect Effect);

public sealed record FeatureFlagGroupOverrideDto(Guid GroupId, DecisionEffect Effect);