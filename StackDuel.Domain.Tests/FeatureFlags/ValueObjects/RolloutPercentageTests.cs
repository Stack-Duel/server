using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Domain.Tests.FeatureFlags.ValueObjects;

public class RolloutPercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(99)]
    [InlineData(100)]
    public void Constructor_ValueInRange_Succeeds(int value)
    {
        Assert.Equal(value, new RolloutPercentage(value).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_ValueOutOfRange_Throws(int value)
    {
        Assert.Throws<ArgumentException>(() => new RolloutPercentage(value));
    }

    [Fact]
    public void Zero_HasValueZero()
    {
        Assert.Equal(0, RolloutPercentage.Zero.Value);
    }
}