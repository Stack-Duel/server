using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class TitleTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', Title.MaxLength);

        Assert.Null(Record.Exception(() => new Title(value)));
    }

    [Fact]
    public void Constructor_AtMinLength_Succeeds()
    {
        string value = new('a', Title.MinLength);

        Assert.Null(Record.Exception(() => new Title(value)));
    }

    [Fact]
    public void Constructor_EmptyString_ThrowsInvalidTitleException()
    {
        Assert.Throws<InvalidTitleException>(() => new Title(string.Empty));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidTitleException()
    {
        string value = new('a', Title.MaxLength + 1);

        Assert.Throws<InvalidTitleException>(() => new Title(value));
    }

    [Fact]
    public void Constructor_BelowMinLength_ThrowsInvalidTitleException()
    {
        string value = new('a', Title.MinLength - 1);

        Assert.Throws<InvalidTitleException>(() => new Title(value));
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ThrowsInvalidTitleException()
    {
        Assert.Throws<InvalidTitleException>(() => new Title("   "));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Title("Two Sum");
        var b = new Title("Three Sum");

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Title("Two Sum");
        var b = new Title("Two Sum");

        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var title = new Title("Two Sum");

        string result = title;

        Assert.Equal("Two Sum", result);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var title = new Title("Two Sum");

        Assert.Equal("Two Sum", title.ToString());
    }
}