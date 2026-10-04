using StackDuel.Domain.Languages.Exceptions;
using StackDuel.Domain.Languages.ValueObjects;

namespace StackDuel.Domain.Tests.Language.ValueObjects;

public class LanguageSlugTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', LanguageSlug.MaxLength);

        Assert.That(() => new LanguageSlug(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_AtMinLength_Succeeds()
    {
        Assert.That(() => new LanguageSlug("c"), Throws.Nothing);
    }

    [Test]
    public void Constructor_EmptyString_ThrowsInvalidLanguageSlugException()
    {
        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug(string.Empty));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidLanguageSlugException()
    {
        string value = new('a', LanguageSlug.MaxLength + 1);

        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug(value));
    }

    [Test]
    public void Constructor_WhitespaceOnly_ThrowsInvalidLanguageSlugException()
    {
        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug("   "));
    }

    [TestCase("Python")]
    [TestCase("PYTHON")]
    [TestCase("-python")]
    [TestCase("python-")]
    [TestCase("python--311")]
    [TestCase("python 311")]
    public void Constructor_InvalidFormat_ThrowsInvalidLanguageSlugException(string value)
    {
        Assert.Throws<InvalidLanguageSlugException>(() => new LanguageSlug(value));
    }

    [TestCase("python")]
    [TestCase("cpp")]
    [TestCase("python-311")]
    [TestCase("c")]
    public void Constructor_ValidFormat_Succeeds(string value)
    {
        Assert.That(() => new LanguageSlug(value), Throws.Nothing);
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new LanguageSlug("python");
        var b = new LanguageSlug("cpp");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new LanguageSlug("python");
        var b = new LanguageSlug("python");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void FromName_CollapseMultipleSpaces()
    {
        var name = new LanguageName("C  Sharp");

        var slug = LanguageSlug.FromName(name);

        Assert.That(slug.Value, Is.EqualTo("c-sharp"));
    }

    [Test]
    public void FromName_GeneratesValidSlug()
    {
        var name = new LanguageName("Python");

        var slug = LanguageSlug.FromName(name);

        Assert.That(slug.Value, Is.EqualTo("python"));
    }

    [Test]
    public void FromName_StripsSpecialCharacters()
    {
        var name = new LanguageName("C++");

        var slug = LanguageSlug.FromName(name);

        Assert.That(slug.Value, Is.EqualTo("c"));
    }

    [Test]
    public void ImplicitConversion_ReturnsValue()
    {
        var slug = new LanguageSlug("python");

        string result = slug;

        Assert.That(result, Is.EqualTo("python"));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var slug = new LanguageSlug("python");

        Assert.That(slug.ToString(), Is.EqualTo("python"));
    }
}