using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class DifficultyTests
{
    [Fact]
    public void Constructor_AtMinValue_Succeeds()
    {
        var difficulty = new Difficulty(Difficulty.MinValue);

        Assert.Equal(Difficulty.MinValue, difficulty.Value);
    }

    [Fact]
    public void Constructor_BelowMinValue_ThrowsInvalidDifficultyException()
    {
        Assert.Throws<InvalidDifficultyException>(() => new Difficulty(Difficulty.MinValue - 1));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Difficulty(100);
        var b = new Difficulty(200);

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Difficulty(500);
        var b = new Difficulty(500);

        Assert.Equal(b, a);
    }

    [Theory]
    [InlineData(0, DifficultyTier.Beginner)]
    [InlineData(100, DifficultyTier.Beginner)]
    [InlineData(200, DifficultyTier.Beginner)]
    [InlineData(201, DifficultyTier.Easy)]
    [InlineData(350, DifficultyTier.Easy)]
    [InlineData(500, DifficultyTier.Easy)]
    [InlineData(501, DifficultyTier.Intermediate)]
    [InlineData(750, DifficultyTier.Intermediate)]
    [InlineData(1000, DifficultyTier.Intermediate)]
    [InlineData(1001, DifficultyTier.Advanced)]
    [InlineData(1500, DifficultyTier.Advanced)]
    [InlineData(2000, DifficultyTier.Advanced)]
    [InlineData(2001, DifficultyTier.Expert)]
    [InlineData(3000, DifficultyTier.Expert)]
    public void Tier_ReturnsCorrectTierForValue(int value, DifficultyTier expectedTier)
    {
        var difficulty = new Difficulty(value);

        Assert.Equal(expectedTier, difficulty.Tier);
    }

    [Fact]
    public void ToString_IncludesValueAndTier()
    {
        var difficulty = new Difficulty(100);

        Assert.Contains("100", difficulty.ToString());
        Assert.Contains("Beginner", difficulty.ToString());
    }
}