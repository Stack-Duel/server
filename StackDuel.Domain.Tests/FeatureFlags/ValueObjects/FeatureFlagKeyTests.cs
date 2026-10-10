using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Domain.Tests.FeatureFlags.ValueObjects;

public class FeatureFlagKeyTests
{
    [Theory]
    [InlineData("leaderboards")]
    [InlineData("solo-rush-canary")]
    [InlineData("a")]
    [InlineData("a1-b2")]
    public void Constructor_ValidKebabCase_Succeeds(string value)
    {
        Assert.Equal(value, new FeatureFlagKey(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Leaderboards")]
    [InlineData("leader_boards")]
    [InlineData("leader boards")]
    [InlineData("-leaderboards")]
    [InlineData("leaderboards-")]
    [InlineData("leader--boards")]
    public void Constructor_InvalidFormat_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new FeatureFlagKey(value));
    }

    [Fact]
    public void Constructor_TooLong_Throws()
    {
        string tooLong = new('a', FeatureFlagKey.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => new FeatureFlagKey(tooLong));
    }

    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string maxLength = new('a', FeatureFlagKey.MaxLength);
        Assert.Equal(maxLength, new FeatureFlagKey(maxLength).Value);
    }
}