using StackDuel.Domain.Achievements.ValueObjects;

namespace StackDuel.Domain.Tests.Achievements.ValueObjects;

public class AchievementCodeTests
{
    [TestCase("first-blood")]
    [TestCase("solver")]
    [TestCase("win-streak-10")]
    [TestCase("a")]
    [TestCase("100-solves")]
    public void Constructor_ValidKebabCase_KeepsValue(string value)
    {
        AchievementCode code = new(value);
        Assert.That(code.Value, Is.EqualTo(value));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new AchievementCode(value!));
    }

    [TestCase("First-Blood", Description = "uppercase")]
    [TestCase("first_blood", Description = "underscore separator")]
    [TestCase("first blood", Description = "space separator")]
    [TestCase("-first-blood", Description = "leading hyphen")]
    [TestCase("first-blood-", Description = "trailing hyphen")]
    [TestCase("first--blood", Description = "doubled hyphen")]
    [TestCase("first.blood", Description = "dot separator")]
    public void Constructor_NotKebabCase_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new AchievementCode(value));
    }

    [Test]
    public void Constructor_AtMaxLength_IsAccepted()
    {
        string value = new('a', AchievementCode.MaxLength);
        Assert.That(new AchievementCode(value).Value, Has.Length.EqualTo(AchievementCode.MaxLength));
    }

    [Test]
    public void Constructor_OverMaxLength_Throws()
    {
        string value = new('a', AchievementCode.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => new AchievementCode(value));
    }

    [Test]
    public void Equality_IsByValue()
    {
        Assert.That(new AchievementCode("first-blood"), Is.EqualTo(new AchievementCode("first-blood")));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        Assert.That(new AchievementCode("first-blood"), Is.Not.EqualTo(new AchievementCode("last-blood")));
    }
}