using StackDuel.Application.FeatureFlags;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.FeatureFlags;

namespace StackDuel.Application.Tests.FeatureFlags;

public class FeatureFlagEvaluatorTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Evaluate_UserDenyOverride_BeatsGroupAllowRolloutAndDefault()
    {
        var data = new FeatureFlagEvaluationData(
            "flag",
            DefaultEnabled: true,
            RolloutPercentage: 100,
            DecisionEffect.Deny,
            DecisionEffect.Allow
        );

        Assert.False(FeatureFlagEvaluator.Evaluate(data, UserId));
    }

    [Fact]
    public void Evaluate_UserAllowOverride_BeatsGroupDenyAndDefaultDisabled()
    {
        var data = new FeatureFlagEvaluationData(
            "flag",
            DefaultEnabled: false,
            RolloutPercentage: 0,
            DecisionEffect.Allow,
            DecisionEffect.Deny
        );

        Assert.True(FeatureFlagEvaluator.Evaluate(data, UserId));
    }

    [Fact]
    public void Evaluate_NoUserOverride_GroupDenyBeatsRolloutAndDefault()
    {
        var data = new FeatureFlagEvaluationData(
            "flag",
            DefaultEnabled: true,
            RolloutPercentage: 100,
            UserOverrideEffect: null,
            DecisionEffect.Deny
        );

        Assert.False(FeatureFlagEvaluator.Evaluate(data, UserId));
    }

    [Fact]
    public void Evaluate_NoOverrides_RolloutMatchBeatsDefaultDisabled()
    {
        var data = new FeatureFlagEvaluationData("flag", DefaultEnabled: false, RolloutPercentage: 100, null, null);

        Assert.True(FeatureFlagEvaluator.Evaluate(data, UserId));
    }

    [Fact]
    public void Evaluate_NoOverrides_RolloutNonMatch_FallsThroughToDefault()
    {
        var data = new FeatureFlagEvaluationData("flag", DefaultEnabled: true, RolloutPercentage: 0, null, null);

        Assert.True(FeatureFlagEvaluator.Evaluate(data, UserId));
    }

    [Fact]
    public void Evaluate_AnonymousUser_NeverTriggersRolloutRegardlessOfPercentage()
    {
        var data = new FeatureFlagEvaluationData("flag", DefaultEnabled: false, RolloutPercentage: 100, null, null);

        Assert.False(FeatureFlagEvaluator.Evaluate(data, userId: null));
    }

    [Fact]
    public void Evaluate_AnonymousUser_FallsThroughToDefaultEnabled()
    {
        var data = new FeatureFlagEvaluationData("flag", DefaultEnabled: true, RolloutPercentage: 0, null, null);

        Assert.True(FeatureFlagEvaluator.Evaluate(data, userId: null));
    }
}