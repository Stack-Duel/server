using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;

namespace StackDuel.Domain.Tests.Language.ValueObjects;

public class LanguageNameTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', LanguageName.MaxLength);

        Assert.That(() => new LanguageName(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_AtMinLength_Succeeds()
    {
        Assert.That(() => new LanguageName("C"), Throws.Nothing);
    }

    [Test]
    public void Constructor_EmptyString_ThrowsInvalidLanguageNameException()
    {
        Assert.Throws<InvalidLanguageNameException>(() => new LanguageName(string.Empty));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidLanguageNameException()
    {
        string value = new('a', LanguageName.MaxLength + 1);

        Assert.Throws<InvalidLanguageNameException>(() => new LanguageName(value));
    }

    [Test]
    public void Constructor_WhitespaceOnly_ThrowsInvalidLanguageNameException()
    {
        Assert.Throws<InvalidLanguageNameException>(() => new LanguageName("   "));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new LanguageName("Python");
        var b = new LanguageName("JavaScript");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new LanguageName("Python");
        var b = new LanguageName("Python");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitConversion_ReturnsValue()
    {
        var name = new LanguageName("Python");

        string result = name;

        Assert.That(result, Is.EqualTo("Python"));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var name = new LanguageName("Python");

        Assert.That(name.ToString(), Is.EqualTo("Python"));
    }
}