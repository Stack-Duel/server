using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;
using UserEntity = StackDuel.Domain.Users.Entities.User;

namespace StackDuel.Domain.Tests.User.Entities;

public class UserTests
{
    private static readonly Username ValidUsername = new("alice");
    private const string ValidSub = "auth0|abc123";

    [Fact]
    public void ChangeUsername_DoesNotAffectOtherProperties()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var originalId = user.Id;
        string originalSub = user.Sub;

        user.ChangeUsername(new Username("bob"));

        Assert.Equal(originalId, user.Id);
        Assert.Equal(originalSub, user.Sub);
    }

    [Fact]
    public void ChangeUsername_FirstChange_Succeeds()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var newUsername = new Username("bob");

        user.ChangeUsername(newUsername);

        Assert.Equal(newUsername, user.Username);
    }

    [Fact]
    public void ChangeUsername_SetsUsernameLastChangedAt()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var before = DateTime.UtcNow;

        user.ChangeUsername(new Username("bob"));

        Assert.NotNull(user.UsernameLastChangedAt);
        Assert.True(user.UsernameLastChangedAt >= before);
    }

    [Fact]
    public void ChangeUsername_ValidUsername_UpdatesUsername()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var newUsername = new Username("bob");

        user.ChangeUsername(newUsername);

        Assert.Equal(newUsername, user.Username);
    }

    [Fact]
    public void ChangeUsername_WithinCooldown_ThrowsUsernameCooldownException()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        user.ChangeUsername(new Username("bob"));

        Assert.Throws<UsernameCooldownException>(() => user.ChangeUsername(new Username("charlie")));
    }

    [Fact]
    public void Constructor_BioIsNullByDefault()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.Null(user.Bio);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceSub_ThrowsInvalidUserSubException(string sub)
    {
        Assert.Throws<InvalidUserSubException>(() => new UserEntity(ValidUsername, sub));
    }

    [Fact]
    public void Constructor_GeneratesNonEmptyId()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        var user1 = new UserEntity(ValidUsername, ValidSub);
        var user2 = new UserEntity(ValidUsername, ValidSub);

        Assert.NotEqual(user2.Id, user1.Id);
    }

    [Fact]
    public void Constructor_ImageUrlIsNullByDefault()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.Null(user.ImageUrl);
    }

    [Fact]
    public void Constructor_NullUsername_ThrowsInvalidUsernameException()
    {
        Assert.Throws<InvalidUsernameException>(() => new UserEntity(null!, ValidSub));
    }

    [Fact]
    public void Constructor_SetsSubCorrectly()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.Equal(ValidSub, user.Sub);
    }

    [Fact]
    public void Constructor_SubWithSpecialCharacters_Succeeds()
    {
        var user = new UserEntity(ValidUsername, "google-oauth2|abc.123-xyz");
        Assert.Equal("google-oauth2|abc.123-xyz", user.Sub);
    }

    [Fact]
    public void Constructor_ValidArguments_CreatesUser()
    {
        var user = new UserEntity(ValidUsername, ValidSub);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(ValidSub, user.Sub);
        Assert.Equal(ValidUsername, user.Username);
    }

    [Fact]
    public void Equals_DifferentInstances_AreNotEqual()
    {
        var user1 = new UserEntity(ValidUsername, ValidSub);
        var user2 = new UserEntity(ValidUsername, ValidSub);

        Assert.NotEqual(user2, user1);
    }

    [Fact]
    public void Equals_Null_IsNotEqual()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.False(user.Equals(null));
    }

    [Fact]
    public void Equals_SameInstance_IsEqual()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.True(user.Equals(user));
    }

    [Fact]
    public void GetHashCode_DifferentUsers_ReturnDifferentHashes()
    {
        var user1 = new UserEntity(ValidUsername, ValidSub);
        var user2 = new UserEntity(ValidUsername, ValidSub);

        Assert.NotEqual(user2.GetHashCode(), user1.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameUser_ReturnsSameHash()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        int hash1 = user.GetHashCode();
        int hash2 = user.GetHashCode();
        Assert.Equal(hash2, hash1);
    }

    [Fact]
    public void UpdateBio_DoesNotAffectOtherProperties()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var originalId = user.Id;
        string originalSub = user.Sub;

        user.UpdateBio(new Bio("Some bio."));

        Assert.Equal(originalId, user.Id);
        Assert.Equal(originalSub, user.Sub);
    }

    [Fact]
    public void UpdateBio_Null_ClearsBio()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        user.UpdateBio(new Bio("Some bio."));

        user.UpdateBio(null);

        Assert.Null(user.Bio);
    }

    [Fact]
    public void UpdateBio_ValidBio_SetsBio()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var bio = new Bio("I love competitive programming.");

        user.UpdateBio(bio);

        Assert.Equal(bio, user.Bio);
    }

    [Fact]
    public void UpdateImageUrl_Null_ClearsImageUrl()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        user.UpdateImageUrl(new ImageUrl("https://example.com/avatar.png"));

        user.UpdateImageUrl(null);

        Assert.Null(user.ImageUrl);
    }

    [Fact]
    public void UpdateImageUrl_ValidUrl_SetsImageUrl()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var imageUrl = new ImageUrl("https://example.com/avatar.png");

        user.UpdateImageUrl(imageUrl);

        Assert.Equal(imageUrl, user.ImageUrl);
    }
}