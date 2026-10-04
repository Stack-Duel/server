using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.FeatureFlags;

namespace StackDuel.Application.FeatureFlags;

public static class FeatureFlagEvaluator
{
    public static bool Evaluate(FeatureFlagEvaluationData data, Guid? userId)
    {
        if (data.UserOverrideEffect == DecisionEffect.Deny)
            return false;
        if (data.UserOverrideEffect == DecisionEffect.Allow)
            return true;

        if (data.GroupOverrideEffect == DecisionEffect.Deny)
            return false;
        if (data.GroupOverrideEffect == DecisionEffect.Allow)
            return true;

        if (userId is Guid uid && FeatureFlagBucketing.IsInRollout(data.Key, uid, data.RolloutPercentage))
            return true;

        return data.DefaultEnabled;
    }
}