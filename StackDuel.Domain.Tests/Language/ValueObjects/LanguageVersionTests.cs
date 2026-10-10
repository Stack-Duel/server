using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;

namespace StackDuel.Domain.Tests.Language.ValueObjects;

public class LanguageVersionTests
{
    [Fact]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', LanguageVersion.MaxLength);

        Assert.Null(Record.Exception(() => new LanguageVersion(value)));
    }

    [Fact]
    public void Constructor_EmptyString_ThrowsInvalidLanguageVersionException()
    {
        Assert.Throws<InvalidLanguageVersionException>(() => new LanguageVersion(string.Empty));
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidLanguageVersionException()
    {
        string value = new('a', LanguageVersion.MaxLength + 1);

        Assert.Throws<InvalidLanguageVersionException>(() => new LanguageVersion(value));
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ThrowsInvalidLanguageVersionException()
    {
        Assert.Throws<InvalidLanguageVersionException>(() => new LanguageVersion("   "));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new LanguageVersion("3.11");
        var b = new LanguageVersion("3.12");

        Assert.NotEqual(b, a);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = new LanguageVersion("3.11");
        var b = new LanguageVersion("3.11");

        Assert.Equal(b, a);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var version = new LanguageVersion("3.11");

        string result = version;

        Assert.Equal("3.11", result);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var version = new LanguageVersion("3.11");

        Assert.Equal("3.11", version.ToString());
    }
}