using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Domain.Tests.FeatureFlags.Entities;

public class FeatureFlagTests
{
    private static FeatureFlag CreateFlag(bool defaultEnabled = false) =>
        FeatureFlag.Create(new FeatureFlagKey("leaderboards"), "Leaderboards", "Global kill-switch.", defaultEnabled);

    [Test]
    public void Create_SetsRolloutPercentageToZero()
    {
        FeatureFlag flag = CreateFlag();
        Assert.That(flag.RolloutPercentage, Is.EqualTo(RolloutPercentage.Zero));
    }

    [Test]
    public void Create_SetsCreatedAtAndUpdatedAtToSameInstant()
    {
        FeatureFlag flag = CreateFlag();
        Assert.That(flag.CreatedAt, Is.EqualTo(flag.UpdatedAt));
    }

    [Test]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            FeatureFlag.Create(new FeatureFlagKey("leaderboards"), "  ", "desc", false)
        );
    }

    [Test]
    public void SetDefaultEnabled_UpdatesValueAndBumpsUpdatedAt()
    {
        FeatureFlag flag = CreateFlag(defaultEnabled: false);
        DateTimeOffset originalUpdatedAt = flag.UpdatedAt;

        flag.SetDefaultEnabled(true);

        Assert.Multiple(() =>
        {
            Assert.That(flag.DefaultEnabled, Is.True);
            Assert.That(flag.UpdatedAt, Is.GreaterThanOrEqualTo(originalUpdatedAt));
        });
    }

    [Test]
    public void SetUserOverride_NewUser_AddsOverride()
    {
        FeatureFlag flag = CreateFlag();
        UserId userId = new(Guid.NewGuid());

        flag.SetUserOverride(userId, DecisionEffect.Allow);

        Assert.That(flag.UserOverrides, Has.Count.EqualTo(1));
        Assert.That(flag.UserOverrides.Single().Effect, Is.EqualTo(DecisionEffect.Allow));
    }

    [Test]
    public void SetUserOverride_ExistingUser_UpdatesEffectInPlaceRatherThanDuplicating()
    {
        FeatureFlag flag = CreateFlag();
        UserId userId = new(Guid.NewGuid());

        flag.SetUserOverride(userId, DecisionEffect.Allow);
        flag.SetUserOverride(userId, DecisionEffect.Deny);

        Assert.That(flag.UserOverrides, Has.Count.EqualTo(1));
        Assert.That(flag.UserOverrides.Single().Effect, Is.EqualTo(DecisionEffect.Deny));
    }

    [Test]
    public void RemoveUserOverride_RemovesMatchingOverride()
    {
        FeatureFlag flag = CreateFlag();
        UserId userId = new(Guid.NewGuid());
        flag.SetUserOverride(userId, DecisionEffect.Allow);

        flag.RemoveUserOverride(userId);

        Assert.That(flag.UserOverrides, Is.Empty);
    }

    [Test]
    public void SetGroupOverride_ExistingGroup_UpdatesEffectInPlaceRatherThanDuplicating()
    {
        FeatureFlag flag = CreateFlag();
        GroupId groupId = new(Guid.NewGuid());

        flag.SetGroupOverride(groupId, DecisionEffect.Deny);
        flag.SetGroupOverride(groupId, DecisionEffect.Allow);

        Assert.That(flag.GroupOverrides, Has.Count.EqualTo(1));
        Assert.That(flag.GroupOverrides.Single().Effect, Is.EqualTo(DecisionEffect.Allow));
    }

    [Test]
    public void RemoveGroupOverride_RemovesMatchingOverride()
    {
        FeatureFlag flag = CreateFlag();
        GroupId groupId = new(Guid.NewGuid());
        flag.SetGroupOverride(groupId, DecisionEffect.Allow);

        flag.RemoveGroupOverride(groupId);

        Assert.That(flag.GroupOverrides, Is.Empty);
    }
}