using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class SlugTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string repeated = string.Concat(Enumerable.Repeat("a", Slug.MaxLength));

        Assert.That(() => new Slug(repeated), Throws.Nothing);
    }

    [Test]
    public void Constructor_AtMinLength_Succeeds()
    {
        string value = new('a', Slug.MinLength);

        Assert.That(() => new Slug(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_BelowMinLength_ThrowsInvalidSlugException()
    {
        string value = new('a', Slug.MinLength - 1);

        Assert.Throws<InvalidSlugException>(() => new Slug(value));
    }

    [Test]
    public void Constructor_EmptyString_ThrowsInvalidSlugException()
    {
        Assert.Throws<InvalidSlugException>(() => new Slug(string.Empty));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidSlugException()
    {
        string value = new('a', Slug.MaxLength + 1);

        Assert.Throws<InvalidSlugException>(() => new Slug(value));
    }

    [Test]
    [TestCase("Two-Sum")]
    [TestCase("TWOSUM")]
    [TestCase("-two-sum")]
    [TestCase("two-sum-")]
    [TestCase("two--sum")]
    [TestCase("two sum")]
    public void Constructor_InvalidFormat_ThrowsInvalidSlugException(string value)
    {
        Assert.Throws<InvalidSlugException>(() => new Slug(value));
    }

    [Test]
    [TestCase("two-sum")]
    [TestCase("twosum")]
    [TestCase("two-sum-123")]
    [TestCase("abc")]
    public void Constructor_ValidFormat_Succeeds(string value)
    {
        Assert.That(() => new Slug(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_WhitespaceOnly_ThrowsInvalidSlugException()
    {
        Assert.Throws<InvalidSlugException>(() => new Slug("   "));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Slug("two-sum");
        var b = new Slug("three-sum");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Slug("two-sum");
        var b = new Slug("two-sum");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void FromTitle_GeneratesValidSlug()
    {
        var title = new Title("Two Sum");

        var slug = Slug.FromTitle(title);

        Assert.That(slug.Value, Is.EqualTo("two-sum"));
    }

    [Test]
    public void FromTitle_StripsSpecialCharacters()
    {
        var title = new Title("Two Sum!");

        var slug = Slug.FromTitle(title);

        Assert.That(slug.Value, Is.EqualTo("two-sum"));
    }

    [Test]
    public void FromTitle_CollapseMultipleSpaces()
    {
        var title = new Title("Two  Sum");

        var slug = Slug.FromTitle(title);

        Assert.That(slug.Value, Is.EqualTo("two-sum"));
    }

    [Test]
    public void ImplicitConversion_ReturnsValue()
    {
        var slug = new Slug("two-sum");

        string result = slug;

        Assert.That(result, Is.EqualTo("two-sum"));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var slug = new Slug("two-sum");

        Assert.That(slug.ToString(), Is.EqualTo("two-sum"));
    }
}