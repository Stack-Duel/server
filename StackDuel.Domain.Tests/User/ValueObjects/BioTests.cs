using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Tests.User.ValueObjects;

public class BioTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string atMax = new('a', Bio.MaxLength);
        Assert.DoesNotThrow(() => new Bio(atMax));
    }

    [Test]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidBioException([Values("", " ", "   ", null)] string? value)
    {
        Assert.Throws<InvalidBioException>(() => new Bio(value!));
    }

    [Test]
    public void Constructor_FarExceedsMaxLength_ThrowsInvalidBioException()
    {
        string veryLong = new('a', 10000);
        Assert.Throws<InvalidBioException>(() => new Bio(veryLong));
    }

    [Test]
    public void Constructor_OneAboveMaxLength_ThrowsInvalidBioException()
    {
        string tooLong = new('a', Bio.MaxLength + 1);
        Assert.Throws<InvalidBioException>(() => new Bio(tooLong));
    }

    [Test]
    public void Constructor_ValidValue_SetsValue()
    {
        var bio = new Bio("I love competitive programming.");
        Assert.That(bio.Value, Is.EqualTo("I love competitive programming."));
    }

    [Test]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = new Bio("Hello world.");
        var b = new Bio("Goodbye world.");
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Bio("Hello world.");
        var b = new Bio("Hello world.");
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var bio = new Bio("Hello world.");
        string value = bio;
        Assert.That(value, Is.EqualTo("Hello world."));
    }

    [Test]
    public void ToString_MatchesImplicitOperator()
    {
        var bio = new Bio("Hello world.");
        Assert.That(bio.ToString(), Is.EqualTo((string)bio));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var bio = new Bio("Hello world.");
        Assert.That(bio.ToString(), Is.EqualTo("Hello world."));
    }
}