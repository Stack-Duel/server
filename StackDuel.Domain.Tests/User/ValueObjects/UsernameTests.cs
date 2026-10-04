using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Domain.Tests.User.ValueObjects;

public class UsernameTests
{
    [Test]
    public void Constructor_AtMaxLength_Succeeds()
    {
        string atMax = new('a', Username.MaxLength);
        Assert.DoesNotThrow(() => new Username(atMax));
    }

    [Test]
    public void Constructor_AtMinLength_Succeeds()
    {
        string atMin = new('a', Username.MinLength);
        Assert.DoesNotThrow(() => new Username(atMin));
    }

    [Test]
    public void Constructor_EmptyOrWhitespace_ThrowsInvalidUsernameException(
        [Values("", " ", "   ", null)] string? value
    )
    {
        Assert.Throws<InvalidUsernameException>(() => new Username(value!));
    }

    [Test]
    public void Constructor_FarExceedsMaxLength_ThrowsInvalidUsernameException()
    {
        string veryLong = new('a', 1000);
        Assert.Throws<InvalidUsernameException>(() => new Username(veryLong));
    }

    [Test]
    public void Constructor_OneAboveMaxLength_ThrowsInvalidUsernameException()
    {
        string tooLong = new('a', Username.MaxLength + 1);
        Assert.Throws<InvalidUsernameException>(() => new Username(tooLong));
    }

    [Test]
    public void Constructor_OneBelowMinLength_ThrowsInvalidUsernameException()
    {
        string tooShort = new('a', Username.MinLength - 1);
        Assert.Throws<InvalidUsernameException>(() => new Username(tooShort));
    }

    [Test]
    public void Constructor_ValidCharacterVariations_Succeeds(
        [Values("alice123", "ALICE", "Alice", "123", "a")] string value
    )
    {
        Assert.DoesNotThrow(() => new Username(value));
    }

    [Test]
    public void Constructor_ValidValue_SetsValue()
    {
        var username = new Username("alice");
        Assert.That(username.Value, Is.EqualTo("alice"));
    }

    [Test]
    public void Equality_DifferentCasing_AreNotEqual()
    {
        var a = new Username("alice");
        var b = new Username("Alice");
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var a = new Username("alice");
        var b = new Username("bob");
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_SameReference_AreEqual()
    {
        var a = new Username("alice");
        Assert.That(a.Equals(a), Is.True);
    }

    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = new Username("alice");
        var b = new Username("alice");
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ImplicitOperator_ReturnsStringValue()
    {
        var username = new Username("alice");
        string value = username;
        Assert.That(value, Is.EqualTo("alice"));
    }

    [Test]
    public void ToString_MatchesImplicitOperator()
    {
        var username = new Username("alice");
        Assert.That(username.ToString(), Is.EqualTo((string)username));
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        var username = new Username("alice");
        Assert.That(username.ToString(), Is.EqualTo("alice"));
    }
}