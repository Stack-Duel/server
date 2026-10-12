using StackDuel.Application.FeatureFlags;

namespace StackDuel.Application.Tests.FeatureFlags;

public class FeatureFlagBucketingTests
{
    [Fact]
    public void GetBucket_SameInputs_AlwaysReturnsSameBucket()
    {
        Guid userId = Guid.NewGuid();

        int first = FeatureFlagBucketing.GetBucket("leaderboards", userId);
        int second = FeatureFlagBucketing.GetBucket("leaderboards", userId);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetBucket_ReturnsValueInZeroToNinetyNineRange()
    {
        for (int i = 0; i < 1000; i++)
        {
            int bucket = FeatureFlagBucketing.GetBucket("flag", Guid.NewGuid());
            Assert.InRange(bucket, 0, 99);
        }
    }

    [Fact]
    public void GetBucket_DifferentFlagKeys_ProduceDifferentBucketsForSameUser()
    {
        Guid userId = Guid.NewGuid();

        int bucketA = FeatureFlagBucketing.GetBucket("flag-a", userId);
        int bucketB = FeatureFlagBucketing.GetBucket("flag-b", userId);

        Assert.NotEqual(bucketB, bucketA);
    }

    [Fact]
    public void IsInRollout_ZeroPercent_NeverEnabled()
    {
        for (int i = 0; i < 100; i++)
        {
            Assert.False(FeatureFlagBucketing.IsInRollout("flag", Guid.NewGuid(), 0));
        }
    }

    [Fact]
    public void IsInRollout_HundredPercent_AlwaysEnabled()
    {
        for (int i = 0; i < 100; i++)
        {
            Assert.True(FeatureFlagBucketing.IsInRollout("flag", Guid.NewGuid(), 100));
        }
    }

    [Fact]
    public void IsInRollout_TenPercent_EnablesRoughlyTenPercentOfUsers()
    {
        const int sampleSize = 10_000;
        int enabledCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            if (FeatureFlagBucketing.IsInRollout("flag", Guid.NewGuid(), 10))
                enabledCount++;
        }

        double enabledPercentage = enabledCount / (double)sampleSize * 100;

        Assert.InRange(enabledPercentage, 8.0, 12.0);
    }

    [Fact]
    public void IsInRollout_SameUserAndFlag_IsStickyAcrossRepeatedCalls()
    {
        Guid userId = Guid.NewGuid();

        bool first = FeatureFlagBucketing.IsInRollout("flag", userId, 50);
        bool second = FeatureFlagBucketing.IsInRollout("flag", userId, 50);
        bool third = FeatureFlagBucketing.IsInRollout("flag", userId, 50);

        Assert.All(new[] { first, second, third }, item => Assert.Equal(first, item));
    }
}