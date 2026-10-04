using StackDuel.Domain.User.Exceptions;
using StackDuel.Domain.User.ValueObjects;

namespace StackDuel.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Constructor_SetsRequiredFields()
    {
        var user = new StackDuel.Domain.User.Entities.User(new Username("alice"), new UserSub("sub-1"), "https://example.com/a.png", "tenant-1");

        Assert.Equal("alice", user.Username.Value);
        Assert.Equal("sub-1", user.Sub.Value);
        Assert.Equal("https://example.com/a.png", user.ImageUrl?.Value);
        Assert.Equal("tenant-1", user.Tenant);
    }

    [Fact]
    public void ChangeUsername_WithinCooldown_Throws()
    {
        var user = new StackDuel.Domain.User.Entities.User(new Username("alice"), new UserSub("sub-1"));
        user.ChangeUsername(new Username("alice2"));

        Assert.Throws<UsernameCooldownException>(() => user.ChangeUsername(new Username("alice3")));
    }

    [Fact]
    public void ChangeUsername_UpdatesUsernameAndTimestamp()
    {
        var user = new StackDuel.Domain.User.Entities.User(new Username("alice"), new UserSub("sub-1"));

        user.ChangeUsername(new Username("alice2"));

        Assert.Equal("alice2", user.Username.Value);
        Assert.NotNull(user.UsernameLastChangedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("this-username-is-way-too-long-to-be-valid")]
    [InlineData("has a space")]
    public void Username_RejectsInvalidValues(string value)
    {
        Assert.Throws<InvalidUsernameException>(() => new Username(value));
    }

    [Fact]
    public void UserSub_RejectsEmpty()
    {
        Assert.Throws<InvalidUserSubException>(() => new UserSub(" "));
    }
}