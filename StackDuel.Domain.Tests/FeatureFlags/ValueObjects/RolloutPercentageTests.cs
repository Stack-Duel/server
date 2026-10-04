using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Domain.Tests.FeatureFlags.ValueObjects;

public class RolloutPercentageTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(50)]
    [TestCase(99)]
    [TestCase(100)]
    public void Constructor_ValueInRange_Succeeds(int value)
    {
        Assert.That(new RolloutPercentage(value).Value, Is.EqualTo(value));
    }

    [TestCase(-1)]
    [TestCase(101)]
    public void Constructor_ValueOutOfRange_Throws(int value)
    {
        Assert.Throws<ArgumentException>(() => new RolloutPercentage(value));
    }

    [Test]
    public void Zero_HasValueZero()
    {
        Assert.That(RolloutPercentage.Zero.Value, Is.EqualTo(0));
    }
}