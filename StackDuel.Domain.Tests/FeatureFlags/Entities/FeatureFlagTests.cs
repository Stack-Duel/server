using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Domain.Tests.FeatureFlags.Entities;

public class FeatureFlagTests
{
    private static FeatureFlag CreateFlag(bool defaultEnabled = false) =>
        FeatureFlag.Create(new FeatureFlagKey("leaderboards"), "Leaderboards", "Global kill-switch.", defaultEnabled);

    [Fact]
    public void Create_SetsRolloutPercentageToZero()
    {
        FeatureFlag flag = CreateFlag();
        Assert.Equal(RolloutPercentage.Zero, flag.RolloutPercentage);
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtToSameInstant()
    {
        FeatureFlag flag = CreateFlag();
        Assert.Equal(flag.UpdatedAt, flag.CreatedAt);
    }

    [Fact]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            FeatureFlag.Create(new FeatureFlagKey("leaderboards"), "  ", "desc", false)
        );
    }

    [Fact]
    public void SetDefaultEnabled_UpdatesValueAndBumpsUpdatedAt()
    {
        FeatureFlag flag = CreateFlag(defaultEnabled: false);
        DateTimeOffset originalUpdatedAt = flag.UpdatedAt;

        flag.SetDefaultEnabled(true);

        Assert.True(flag.DefaultEnabled);
        Assert.True(flag.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void SetUserOverride_NewUser_AddsOverride()
    {
        FeatureFlag flag = CreateFlag();
        UserId userId = new(Guid.NewGuid());

        flag.SetUserOverride(userId, DecisionEffect.Allow);

        Assert.Single(flag.UserOverrides);
        Assert.Equal(DecisionEffect.Allow, flag.UserOverrides.Single().Effect);
    }

    [Fact]
    public void SetUserOverride_ExistingUser_UpdatesEffectInPlaceRatherThanDuplicating()
    {
        FeatureFlag flag = CreateFlag();
        UserId userId = new(Guid.NewGuid());

        flag.SetUserOverride(userId, DecisionEffect.Allow);
        flag.SetUserOverride(userId, DecisionEffect.Deny);

        Assert.Single(flag.UserOverrides);
        Assert.Equal(DecisionEffect.Deny, flag.UserOverrides.Single().Effect);
    }

    [Fact]
    public void RemoveUserOverride_RemovesMatchingOverride()
    {
        FeatureFlag flag = CreateFlag();
        UserId userId = new(Guid.NewGuid());
        flag.SetUserOverride(userId, DecisionEffect.Allow);

        flag.RemoveUserOverride(userId);

        Assert.Empty(flag.UserOverrides);
    }

    [Fact]
    public void SetGroupOverride_ExistingGroup_UpdatesEffectInPlaceRatherThanDuplicating()
    {
        FeatureFlag flag = CreateFlag();
        GroupId groupId = new(Guid.NewGuid());

        flag.SetGroupOverride(groupId, DecisionEffect.Deny);
        flag.SetGroupOverride(groupId, DecisionEffect.Allow);

        Assert.Single(flag.GroupOverrides);
        Assert.Equal(DecisionEffect.Allow, flag.GroupOverrides.Single().Effect);
    }

    [Fact]
    public void RemoveGroupOverride_RemovesMatchingOverride()
    {
        FeatureFlag flag = CreateFlag();
        GroupId groupId = new(Guid.NewGuid());
        flag.SetGroupOverride(groupId, DecisionEffect.Allow);

        flag.RemoveGroupOverride(groupId);

        Assert.Empty(flag.GroupOverrides);
    }
}