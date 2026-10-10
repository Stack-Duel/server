using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;

namespace StackDuel.Domain.Tests.Language.ValueObjects;

public class LanguageSlugTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', LanguageSlug.MaxLength);

        Assert.Null(Record.Exception(() => new LanguageSlug(value)));
    }

    [Fact]
    public void Constructor_AtMinLength_Succeeds()
    {
        Assert.Null(Record.Exception(() => new LanguageSlug("c")));
    }

    [Fact]
    public void Constructor_EmptyString_ThrowsInvalidLanguageSlugException()
    {
        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug(string.Empty));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidLanguageSlugException()
    {
        string value = new('a', LanguageSlug.MaxLength + 1);

        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug(value));
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ThrowsInvalidLanguageSlugException()
    {
        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug("   "));
    }

    [Theory]
    [InlineData("Python")]
    [InlineData("PYTHON")]
    [InlineData("-python")]
    [InlineData("python-")]
    [InlineData("python--311")]
    [InlineData("python 311")]
    public void Constructor_InvalidFormat_ThrowsInvalidLanguageSlugException(string value)
    {
        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug(value));
    }

    [Theory]
    [InlineData("python")]
    [InlineData("cpp")]
    [InlineData("python-311")]
    [InlineData("c")]
    public void Constructor_ValidFormat_Succeeds(string value)
    {
        Assert.Null(Record.Exception(() => new LanguageSlug(value)));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new LanguageSlug("python");
        var b = new LanguageSlug("cpp");

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new LanguageSlug("python");
        var b = new LanguageSlug("python");

        Assert.Equal(b, a);
    }

    [Fact]
    public void FromName_CollapseMultipleSpaces()
    {
        var name = new LanguageName("C  Sharp");

        var slug = LanguageSlug.FromName(name);

        Assert.Equal("c-sharp", slug.Value);
    }

    [Fact]
    public void FromName_GeneratesValidSlug()
    {
        var name = new LanguageName("Python");

        var slug = LanguageSlug.FromName(name);

        Assert.Equal("python", slug.Value);
    }

    [Fact]
    public void FromName_StripsSpecialCharacters()
    {
        var name = new LanguageName("C++");

        var slug = LanguageSlug.FromName(name);

        Assert.Equal("c", slug.Value);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var slug = new LanguageSlug("python");

        string result = slug;

        Assert.Equal("python", result);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var slug = new LanguageSlug("python");

        Assert.Equal("python", slug.ToString());
    }
}