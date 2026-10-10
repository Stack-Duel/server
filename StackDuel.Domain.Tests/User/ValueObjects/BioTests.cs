using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Tests.User.ValueObjects;

public class BioTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string atMax = new('a', Bio.MaxLength);
        Assert.Null(Record.Exception(() => new Bio(atMax)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidBioException(string? value)
    {
        Assert.Throws<InvalidBioException>(() => new Bio(value!));
    }

    [Fact]
    public void Constructor_FarExceedsMaxLength_ThrowsInvalidBioException()
    {
        string veryLong = new('a', 10000);
        Assert.Throws<InvalidBioException>(() => new Bio(veryLong));
    }

    [Fact]
    public void Constructor_OneAboveMaxLength_ThrowsInvalidBioException()
    {
        string tooLong = new('a', Bio.MaxLength + 1);
        Assert.Throws<InvalidBioException>(() => new Bio(tooLong));
    }

    [Fact]
    public void Constructor_ValidValue_SetsValue()
    {
        var bio = new Bio("I love competitive programming.");
        Assert.Equal("I love competitive programming.", bio.Value);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = new Bio("Hello world.");
        var b = new Bio("Goodbye world.");
        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Bio("Hello world.");
        var b = new Bio("Hello world.");
        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var bio = new Bio("Hello world.");
        string value = bio;
        Assert.Equal("Hello world.", value);
    }

    [Fact]
    public void ToString_MatchesImplicitOperator()
    {
        var bio = new Bio("Hello world.");
        Assert.Equal((string)bio, bio.ToString());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var bio = new Bio("Hello world.");
        Assert.Equal("Hello world.", bio.ToString());
    }
}