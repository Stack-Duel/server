using StackDuel.Domain.Problems.Exceptions;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Domain.Tests.Problem.ValueObjects;

public class TitleTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string value = new('a', Title.MaxLength);

        Assert.That(() => new Title(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_AtMinLength_Succeeds()
    {
        string value = new('a', Title.MinLength);

        Assert.That(() => new Title(value), Throws.Nothing);
    }

    [Test]
    public void Constructor_EmptyString_ThrowsInvalidTitleException()
    {
        Assert.Throws<InvalidTitleException>(() => new Title(string.Empty));
    }

    [Test]
    public void Constructor_ExceedsMaxLength_ThrowsInvalidTitleException()
    {
        string value = new('a', Title.MaxLength + 1);

        Assert.Throws<InvalidTitleException>(() => new Title(value));
    }

    [Test]
    public void Constructor_BelowMinLength_ThrowsInvalidTitleException()
    {
        string value = new('a', Title.MinLength - 1);

        Assert.Throws<InvalidTitleException>(() => new Title(value));
    }

    [Test]
    public void Constructor_WhitespaceOnly_ThrowsInvalidTitleException()
    {
        Assert.Throws<InvalidTitleException>(() => new Title("   "));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Title("Two Sum");
        var b = new Title("Three Sum");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Title("Two Sum");
        var b = new Title("Two Sum");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitConversion_ReturnsValue()
    {
        var title = new Title("Two Sum");

        string result = title;

        Assert.That(result, Is.EqualTo("Two Sum"));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var title = new Title("Two Sum");

        Assert.That(title.ToString(), Is.EqualTo("Two Sum"));
    }
}