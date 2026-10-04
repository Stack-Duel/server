using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Domain.FeatureFlags;

public interface IFeatureFlagEvaluationRepository
{
    Task<FeatureFlagEvaluationData?> GetEvaluationDataAsync(
        string flagKey,
        Guid? userId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyDictionary<string, FeatureFlagEvaluationData>> GetAllEvaluationDataAsync(
        Guid? userId,
        CancellationToken cancellationToken
    );
}

public sealed record FeatureFlagEvaluationData(
    string Key,
    bool DefaultEnabled,
    int RolloutPercentage,
    DecisionEffect? UserOverrideEffect,
    DecisionEffect? GroupOverrideEffect
);