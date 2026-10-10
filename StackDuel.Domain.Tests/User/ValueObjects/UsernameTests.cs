using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Tests.User.ValueObjects;

public class UsernameTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string atMax = new('a', Username.MaxLength);
        Assert.Null(Record.Exception(() => new Username(atMax)));
    }

    [Fact]
    public void Constructor_AtMinLength_Succeeds()
    {
        string atMin = new('a', Username.MinLength);
        Assert.Null(Record.Exception(() => new Username(atMin)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidUsernameException(string? value)
    {
        Assert.Throws<InvalidUsernameException>(() => new Username(value!));
    }

    [Fact]
    public void Constructor_FarExceedsMaxLength_ThrowsInvalidUsernameException()
    {
        string veryLong = new('a', 1000);
        Assert.Throws<InvalidUsernameException>(() => new Username(veryLong));
    }

    [Fact]
    public void Constructor_OneAboveMaxLength_ThrowsInvalidUsernameException()
    {
        string tooLong = new('a', Username.MaxLength + 1);
        Assert.Throws<InvalidUsernameException>(() => new Username(tooLong));
    }

    [Fact]
    public void Constructor_OneBelowMinLength_ThrowsInvalidUsernameException()
    {
        string tooShort = new('a', Username.MinLength - 1);
        Assert.Throws<InvalidUsernameException>(() => new Username(tooShort));
    }

    [Theory]
    [InlineData("alice123")]
    [InlineData("ALICE")]
    [InlineData("Alice")]
    [InlineData("123")]
    [InlineData("a")]
    public void Constructor_ValidCharacterVariations_Succeeds(string value)
    {
        Assert.Null(Record.Exception(() => new Username(value)));
    }

    [Fact]
    public void Constructor_ValidValue_SetsValue()
    {
        var username = new Username("alice");
        Assert.Equal("alice", username.Value);
    }

    [Fact]
    public void Equality_DifferentCasing_AreNotEqual()
    {
        var a = new Username("alice");
        var b = new Username("Alice");
        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = new Username("alice");
        var b = new Username("bob");
        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameReference_AreEqual()
    {
        var a = new Username("alice");
        Assert.True(a.Equals(a));
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Username("alice");
        var b = new Username("alice");
        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var username = new Username("alice");
        string value = username;
        Assert.Equal("alice", value);
    }

    [Fact]
    public void ToString_MatchesImplicitOperator()
    {
        var username = new Username("alice");
        Assert.Equal((string)username, username.ToString());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var username = new Username("alice");
        Assert.Equal("alice", username.ToString());
    }
}