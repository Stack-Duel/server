using StackDuel.Domain.Achievements.ValueObjects;

namespace StackDuel.Domain.Tests.Achievements.ValueObjects;

public class AchievementCodeTests
{
    [Theory]
    [InlineData("first-blood")]
    [InlineData("solver")]
    [InlineData("win-streak-10")]
    [InlineData("a")]
    [InlineData("100-solves")]
    public void Constructor_ValidKebabCase_KeepsValue(string value)
    {
        AchievementCode code = new(value);
        Assert.Equal(value, code.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new AchievementCode(value!));
    }

    [Theory]
    [InlineData("First-Blood")] // uppercase
    [InlineData("first_blood")] // underscore separator
    [InlineData("first blood")] // space separator
    [InlineData("-first-blood")] // leading hyphen
    [InlineData("first-blood-")] // trailing hyphen
    [InlineData("first--blood")] // doubled hyphen
    [InlineData("first.blood")] // dot separator
    public void Constructor_NotKebabCase_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new AchievementCode(value));
    }

    [Fact]
    public void Constructor_AtMaxLength_IsAccepted()
    {
        string value = new('a', AchievementCode.MaxLength);
        Assert.Equal(AchievementCode.MaxLength, new AchievementCode(value).Value.Length);
    }

    [Fact]
    public void Constructor_OverMaxLength_Throws()
    {
        string value = new('a', AchievementCode.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => new AchievementCode(value));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        AchievementCode code = new("first-blood");
        AchievementCode sameValue = new("first-blood");

        Assert.Equal(sameValue, code);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        Assert.NotEqual(new AchievementCode("last-blood"), new AchievementCode("first-blood"));
    }
}