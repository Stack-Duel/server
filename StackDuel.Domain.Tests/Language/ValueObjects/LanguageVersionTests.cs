using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;

namespace StackDuel.Domain.Tests.Language.ValueObjects;

public class LanguageVersionTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', LanguageVersion.MaxLength);

        Assert.That(() => new LanguageVersion(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_EmptyString_ThrowsInvalidLanguageVersionException()
    {
        Assert.Throws<InvalidLanguageVersionException>(() => new LanguageVersion(string.Empty));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidLanguageVersionException()
    {
        string value = new('a', LanguageVersion.MaxLength + 1);

        Assert.Throws<InvalidLanguageVersionException>(() => new LanguageVersion(value));
    }

    [Test]
    public void Constructor_WhitespaceOnly_ThrowsInvalidLanguageVersionException()
    {
        Assert.Throws<InvalidLanguageVersionException>(() => new LanguageVersion("   "));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new LanguageVersion("3.11");
        var b = new LanguageVersion("3.12");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new LanguageVersion("3.11");
        var b = new LanguageVersion("3.11");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitConversion_ReturnsValue()
    {
        var version = new LanguageVersion("3.11");

        string result = version;

        Assert.That(result, Is.EqualTo("3.11"));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var version = new LanguageVersion("3.11");

        Assert.That(version.ToString(), Is.EqualTo("3.11"));
    }
}