using StackDuel.Domain.Authorization.Rbac.ValueObjects;

namespace StackDuel.Domain.Tests.Authorization.Rbac.ValueObjects;

public class NameTests
{
    [Test]
    public void Constructor_KeepsValueVerbatim()
    {
        Assert.That(new Name("default-user").Value, Is.EqualTo("default-user"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new Name(value!));
    }

    [Test]
    public void Constructor_AtMaxLength_IsAccepted()
    {
        string value = new('a', Name.MaxLength);
        Assert.That(new Name(value).Value, Has.Length.EqualTo(Name.MaxLength));
    }

    [Test]
    public void Constructor_OverMaxLength_Throws()
    {
        string value = new('a', Name.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => new Name(value));
    }

    [Test]
    public void Equality_IsByValue()
    {
        Name name = new("admin");
        Name sameValue = new("admin");

        Assert.That(name, Is.EqualTo(sameValue));
    }

    [Test]
    public void Equality_IsCaseSensitive()
    {
        Assert.That(new Name("admin"), Is.Not.EqualTo(new Name("Admin")));
    }
}

public class PermissionCodeTests
{
    [Test]
    public void Constructor_KeepsValueVerbatim()
    {
        Assert.That(new PermissionCode("submission:create").Value, Is.EqualTo("submission:create"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new PermissionCode(value!));
    }

    [Test]
    public void Equality_IsByValue()
    {
        PermissionCode code = new("game:duel:play");
        PermissionCode sameValue = new("game:duel:play");

        Assert.That(code, Is.EqualTo(sameValue));
    }

    [Test]
    public void Default_HasNoValue()
    {
        Assert.That(default(PermissionCode).Value, Is.Null);
    }
}