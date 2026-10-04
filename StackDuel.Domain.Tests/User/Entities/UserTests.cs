using StackDuel.Domain.User.Exceptions;
using StackDuel.Domain.User.ValueObjects;
using DomainUser = StackDuel.Domain.User.Entities.User;

namespace StackDuel.Domain.Tests.User.Entities;

public class UserTests
{
    private static Username ValidUsername(string value = "valid_user") => new(value);

    private static UserSub ValidSub(string value = "auth0|123456") => new(value);

    [Fact]
    public void Constructor_SetsUsernameAndSub()
    {
        var username = ValidUsername();
        var sub = ValidSub();

        var sut = new DomainUser(username, sub);

        Assert.Equal(username, sut.Username);
        Assert.Equal(sub, sut.Sub);
    }

    [Fact]
    public void Constructor_WithoutOptionalArguments_LeavesTenantAndImageUrlNull()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());

        Assert.Null(sut.Tenant);
        Assert.Null(sut.ImageUrl);
    }

    [Fact]
    public void Constructor_WithTenant_SetsTenant()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub(), tenant: "acme");

        Assert.Equal("acme", sut.Tenant);
    }

    [Fact]
    public void Constructor_WithImageUrl_SetsImageUrl()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub(), imageUrl: "https://example.com/avatar.png");

        Assert.NotNull(sut.ImageUrl);
        Assert.Equal("https://example.com/avatar.png", sut.ImageUrl!.Value);
    }

    [Fact]
    public void Constructor_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;

        var sut = new DomainUser(ValidUsername(), ValidSub());

        var after = DateTime.UtcNow;
        Assert.InRange(sut.CreatedAt, before, after);
    }

    [Fact]
    public void Constructor_NullUsername_ThrowsInvalidUsernameException()
    {
        Assert.Throws<InvalidUsernameException>(() => new DomainUser(null!, ValidSub()));
    }

    [Fact]
    public void Constructor_NullSub_ThrowsInvalidUserSubException()
    {
        Assert.Throws<InvalidUserSubException>(() => new DomainUser(ValidUsername(), null!));
    }

    [Fact]
    public void ChangeUsername_FirstTime_UpdatesUsernameAndTimestamp()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());
        var newUsername = new Username("new_username");

        sut.ChangeUsername(newUsername);

        Assert.Equal(newUsername, sut.Username);
        Assert.NotNull(sut.UsernameLastChangedAt);
    }

    [Fact]
    public void ChangeUsername_WithinCooldown_ThrowsUsernameCooldownException()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());
        sut.ChangeUsername(new Username("first_change"));

        Assert.Throws<UsernameCooldownException>(() => sut.ChangeUsername(new Username("second_change")));
    }

    [Fact]
    public void ChangeUsername_WithinCooldown_DoesNotChangeUsername()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());
        var firstChange = new Username("first_change");
        sut.ChangeUsername(firstChange);

        try
        {
            sut.ChangeUsername(new Username("second_change"));
        }
        catch (UsernameCooldownException) { }

        Assert.Equal(firstChange, sut.Username);
    }

    [Fact]
    public void UpdateBio_SetsBio()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());
        var bio = new Bio("Hello world");

        sut.UpdateBio(bio);

        Assert.Equal(bio, sut.Bio);
    }

    [Fact]
    public void UpdateBio_Null_ClearsBio()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());
        sut.UpdateBio(new Bio("Hello world"));

        sut.UpdateBio(null);

        Assert.Null(sut.Bio);
    }

    [Fact]
    public void UpdateImageUrl_SetsImageUrl()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());
        var imageUrl = new ImageUrl("https://example.com/avatar.png");

        sut.UpdateImageUrl(imageUrl);

        Assert.Equal(imageUrl, sut.ImageUrl);
    }

    [Fact]
    public void UpdateImageUrl_Null_ClearsImageUrl()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub(), imageUrl: "https://example.com/avatar.png");

        sut.UpdateImageUrl(null);

        Assert.Null(sut.ImageUrl);
    }

    [Fact]
    public void CompleteSetup_SetsSetupCompletedAt()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());

        sut.CompleteSetup();

        Assert.NotNull(sut.SetupCompletedAt);
    }

    [Fact]
    public void CompleteSetup_CalledTwice_DoesNotChangeOriginalTimestamp()
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());
        sut.CompleteSetup();
        var firstCompletedAt = sut.SetupCompletedAt;

        sut.CompleteSetup();

        Assert.Equal(firstCompletedAt, sut.SetupCompletedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetPrivate_SetsIsPrivate(bool isPrivate)
    {
        var sut = new DomainUser(ValidUsername(), ValidSub());

        sut.SetPrivate(isPrivate);

        Assert.Equal(isPrivate, sut.IsPrivate);
    }
}