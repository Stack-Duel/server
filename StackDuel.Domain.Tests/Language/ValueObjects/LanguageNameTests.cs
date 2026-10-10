using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;

namespace StackDuel.Domain.Tests.Language.ValueObjects;

public class LanguageNameTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', LanguageName.MaxLength);

        Assert.Null(Record.Exception(() => new LanguageName(value)));
    }

    [Fact]
    public void Constructor_AtMinLength_Succeeds()
    {
        Assert.Null(Record.Exception(() => new LanguageName("C")));
    }

    [Fact]
    public void Constructor_EmptyString_ThrowsInvalidLanguageNameException()
    {
        Assert.Throws<InvalidLanguageNameException>(() => new LanguageName(string.Empty));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidLanguageNameException()
    {
        string value = new('a', LanguageName.MaxLength + 1);

        Assert.Throws<InvalidLanguageNameException>(() => new LanguageName(value));
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ThrowsInvalidLanguageNameException()
    {
        Assert.Throws<InvalidLanguageNameException>(() => new LanguageName("   "));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new LanguageName("Python");
        var b = new LanguageName("JavaScript");

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new LanguageName("Python");
        var b = new LanguageName("Python");

        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var name = new LanguageName("Python");

        string result = name;

        Assert.Equal("Python", result);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var name = new LanguageName("Python");

        Assert.Equal("Python", name.ToString());
    }
}