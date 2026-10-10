using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class SlugTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string repeated = string.Concat(Enumerable.Repeat("a", Slug.MaxLength));

        Assert.Null(Record.Exception(() => new Slug(repeated)));
    }

    [Fact]
    public void Constructor_AtMinLength_Succeeds()
    {
        string value = new('a', Slug.MinLength);

        Assert.Null(Record.Exception(() => new Slug(value)));
    }

    [Fact]
    public void Constructor_BelowMinLength_ThrowsInvalidSlugException()
    {
        string value = new('a', Slug.MinLength - 1);

        Assert.Throws<InvalidSlugException>(() => new Slug(value));
    }

    [Fact]
    public void Constructor_EmptyString_ThrowsInvalidSlugException()
    {
        Assert.Throws<InvalidSlugException>(() => new Slug(string.Empty));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidSlugException()
    {
        string value = new('a', Slug.MaxLength + 1);

        Assert.Throws<InvalidSlugException>(() => new Slug(value));
    }

    [Theory]
    [InlineData("Two-Sum")]
    [InlineData("TWOSUM")]
    [InlineData("-two-sum")]
    [InlineData("two-sum-")]
    [InlineData("two--sum")]
    [InlineData("two sum")]
    public void Constructor_InvalidFormat_ThrowsInvalidSlugException(string value)
    {
        Assert.Throws<InvalidSlugException>(() => new Slug(value));
    }

    [Theory]
    [InlineData("two-sum")]
    [InlineData("twosum")]
    [InlineData("two-sum-123")]
    [InlineData("abc")]
    public void Constructor_ValidFormat_Succeeds(string value)
    {
        Assert.Null(Record.Exception(() => new Slug(value)));
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ThrowsInvalidSlugException()
    {
        Assert.Throws<InvalidSlugException>(() => new Slug("   "));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Slug("two-sum");
        var b = new Slug("three-sum");

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Slug("two-sum");
        var b = new Slug("two-sum");

        Assert.Equal(b, a);
    }

    [Fact]
    public void FromTitle_GeneratesValidSlug()
    {
        var title = new Title("Two Sum");

        var slug = Slug.FromTitle(title);

        Assert.Equal("two-sum", slug.Value);
    }

    [Fact]
    public void FromTitle_StripsSpecialCharacters()
    {
        var title = new Title("Two Sum!");

        var slug = Slug.FromTitle(title);

        Assert.Equal("two-sum", slug.Value);
    }

    [Fact]
    public void FromTitle_CollapseMultipleSpaces()
    {
        var title = new Title("Two  Sum");

        var slug = Slug.FromTitle(title);

        Assert.Equal("two-sum", slug.Value);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var slug = new Slug("two-sum");

        string result = slug;

        Assert.Equal("two-sum", result);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var slug = new Slug("two-sum");

        Assert.Equal("two-sum", slug.ToString());
    }
}