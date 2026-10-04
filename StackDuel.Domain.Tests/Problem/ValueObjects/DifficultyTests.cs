using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class DifficultyTests
{
    [Test]
    public void Constructor_AtMinValue_Succeeds()
    {
        var difficulty = new Difficulty(Difficulty.MinValue);

        Assert.That(difficulty.Value, Is.EqualTo(Difficulty.MinValue));
    }

    [Test]
    public void Constructor_BelowMinValue_ThrowsInvalidDifficultyException()
    {
        Assert.Throws<InvalidDifficultyException>(() => new Difficulty(Difficulty.MinValue - 1));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Difficulty(100);
        var b = new Difficulty(200);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Difficulty(500);
        var b = new Difficulty(500);

        Assert.That(a, Is.EqualTo(b));
    }

    [TestCase(0, DifficultyTier.Beginner)]
    [TestCase(100, DifficultyTier.Beginner)]
    [TestCase(200, DifficultyTier.Beginner)]
    [TestCase(201, DifficultyTier.Easy)]
    [TestCase(350, DifficultyTier.Easy)]
    [TestCase(500, DifficultyTier.Easy)]
    [TestCase(501, DifficultyTier.Intermediate)]
    [TestCase(750, DifficultyTier.Intermediate)]
    [TestCase(1000, DifficultyTier.Intermediate)]
    [TestCase(1001, DifficultyTier.Advanced)]
    [TestCase(1500, DifficultyTier.Advanced)]
    [TestCase(2000, DifficultyTier.Advanced)]
    [TestCase(2001, DifficultyTier.Expert)]
    [TestCase(3000, DifficultyTier.Expert)]
    public void Tier_ReturnsCorrectTierForValue(int value, DifficultyTier expectedTier)
    {
        var difficulty = new Difficulty(value);

        Assert.That(difficulty.Tier, Is.EqualTo(expectedTier));
    }

    [Test]
    public void ToString_IncludesValueAndTier()
    {
        var difficulty = new Difficulty(100);

        Assert.That(difficulty.ToString(), Does.Contain("100").And.Contain("Beginner"));
    }
}