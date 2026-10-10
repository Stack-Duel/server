using StackDuel.Domain.Authorization.Rbac.ValueObjects;

namespace StackDuel.Domain.Tests.Authorization.Rbac.ValueObjects;

public class NameTests
{
    [Fact]
    public void Constructor_KeepsValueVerbatim()
    {
        Assert.Equal("default-user", new Name("default-user").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new Name(value!));
    }

    [Fact]
    public void Constructor_AtMaxLength_IsAccepted()
    {
        string value = new('a', Name.MaxLength);
        Assert.Equal(Name.MaxLength, new Name(value).Value.Length);
    }

    [Fact]
    public void Constructor_OverMaxLength_Throws()
    {
        string value = new('a', Name.MaxLength + 1);
        Assert.Throws<ArgumentException>(() => new Name(value));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Name name = new("admin");
        Name sameValue = new("admin");

        Assert.Equal(sameValue, name);
    }

    [Fact]
    public void Equality_IsCaseSensitive()
    {
        Assert.NotEqual(new Name("Admin"), new Name("admin"));
    }
}

public class PermissionCodeTests
{
    [Fact]
    public void Constructor_KeepsValueVerbatim()
    {
        Assert.Equal("submission:create", new PermissionCode("submission:create").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankValue_Throws(string? value)
    {
        Assert.Throws<ArgumentException>(() => new PermissionCode(value!));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        PermissionCode code = new("game:duel:play");
        PermissionCode sameValue = new("game:duel:play");

        Assert.Equal(sameValue, code);
    }

    [Fact]
    public void Default_HasNoValue()
    {
        Assert.Null(default(PermissionCode).Value);
    }
}