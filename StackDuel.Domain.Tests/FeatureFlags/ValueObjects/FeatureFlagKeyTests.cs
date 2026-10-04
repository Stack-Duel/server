using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Domain.Tests.FeatureFlags.ValueObjects;

public class FeatureFlagKeyTests
{
    [TestCase("leaderboards")]
    [TestCase("solo-rush-canary")]
    [TestCase("a")]
    [TestCase("a1-b2")]
    public void Constructor_ValidKebabCase_Succeeds(string value)
    {
        Assert.That(new FeatureFlagKey(value).Value, Is.EqualTo(value));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("Leaderboards")]
    [TestCase("leader_boards")]
    [TestCase("leader boards")]
    [TestCase("-leaderboards")]
    [TestCase("leaderboards-")]
    [TestCase("leader--boards")]
    public void Constructor_InvalidFormat_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new FeatureFlagKey(value));
    }

    [Test]
    public void Constructor_TooLong_Throws()
    {
        string tooLong = new('a', FeatureFlagKey.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => new FeatureFlagKey(tooLong));
    }

    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string maxLength = new('a', FeatureFlagKey.MaxLength);
        Assert.That(new FeatureFlagKey(maxLength).Value, Is.EqualTo(maxLength));
    }
}