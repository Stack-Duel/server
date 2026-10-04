using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Api.Requests.FeatureFlags;

public sealed record SetFeatureFlagOverrideRequest(DecisionEffect Effect);