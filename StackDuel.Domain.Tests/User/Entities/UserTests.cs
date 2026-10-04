using StackDuel.Domain.Users.Exceptions;
using StackDuel.Domain.Users.ValueObjects;
using UserEntity = StackDuel.Domain.Users.Entities.User;

namespace StackDuel.Domain.Tests.User.Entities;

public class UserTests
{
    private static readonly Username ValidUsername = new("alice");
    private const string ValidSub = "auth0|abc123";

    [Test]
    public void ChangeUsername_DoesNotAffectOtherProperties()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var originalId = user.Id;
        string originalSub = user.Sub;

        user.ChangeUsername(new Username("bob"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Id, Is.EqualTo(originalId));
            Assert.That(user.Sub, Is.EqualTo(originalSub));
        }
    }

    [Test]
    public void ChangeUsername_FirstChange_Succeeds()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var newUsername = new Username("bob");

        user.ChangeUsername(newUsername);

        Assert.That(user.Username, Is.EqualTo(newUsername));
    }

    [Test]
    public void ChangeUsername_SetsUsernameLastChangedAt()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var before = DateTime.UtcNow;

        user.ChangeUsername(new Username("bob"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.UsernameLastChangedAt, Is.Not.Null);
            Assert.That(user.UsernameLastChangedAt, Is.GreaterThanOrEqualTo(before));
        }
    }

    [Test]
    public void ChangeUsername_ValidUsername_UpdatesUsername()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var newUsername = new Username("bob");

        user.ChangeUsername(newUsername);

        Assert.That(user.Username, Is.EqualTo(newUsername));
    }

    [Test]
    public void ChangeUsername_WithinCooldown_ThrowsUsernameCooldownException()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        user.ChangeUsername(new Username("bob"));

        Assert.Throws<UsernameCooldownException>(() => user.ChangeUsername(new Username("charlie")));
    }

    [Test]
    public void Constructor_BioIsNullByDefault()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.That(user.Bio, Is.Null);
    }

    [Test]
    public void Constructor_EmptyOrWhitespaceSub_ThrowsInvalidUserSubException([Values("", " ", "   ")] string sub)
    {
        Assert.Throws<InvalidUserSubException>(() => new UserEntity(ValidUsername, sub));
    }

    [Test]
    public void Constructor_GeneratesNonEmptyId()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Constructor_GeneratesUniqueIds()
    {
        var user1 = new UserEntity(ValidUsername, ValidSub);
        var user2 = new UserEntity(ValidUsername, ValidSub);

        Assert.That(user1.Id, Is.Not.EqualTo(user2.Id));
    }

    [Test]
    public void Constructor_ImageUrlIsNullByDefault()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.That(user.ImageUrl, Is.Null);
    }

    [Test]
    public void Constructor_NullUsername_ThrowsInvalidUsernameException()
    {
        Assert.Throws<InvalidUsernameException>(() => new UserEntity(null!, ValidSub));
    }

    [Test]
    public void Constructor_SetsSubCorrectly()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.That(user.Sub, Is.EqualTo(ValidSub));
    }

    [Test]
    public void Constructor_SubWithSpecialCharacters_Succeeds()
    {
        var user = new UserEntity(ValidUsername, "google-oauth2|abc.123-xyz");
        Assert.That(user.Sub, Is.EqualTo("google-oauth2|abc.123-xyz"));
    }

    [Test]
    public void Constructor_ValidArguments_CreatesUser()
    {
        var user = new UserEntity(ValidUsername, ValidSub);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(user.Sub, Is.EqualTo(ValidSub));
            Assert.That(user.Username, Is.EqualTo(ValidUsername));
        }
    }

    [Test]
    public void Equals_DifferentInstances_AreNotEqual()
    {
        var user1 = new UserEntity(ValidUsername, ValidSub);
        var user2 = new UserEntity(ValidUsername, ValidSub);

        Assert.That(user1, Is.Not.EqualTo(user2));
    }

    [Test]
    public void Equals_Null_IsNotEqual()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.That(user.Equals(null), Is.False);
    }

    [Test]
    public void Equals_SameInstance_IsEqual()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        Assert.That(user.Equals(user), Is.True);
    }

    [Test]
    public void GetHashCode_DifferentUsers_ReturnDifferentHashes()
    {
        var user1 = new UserEntity(ValidUsername, ValidSub);
        var user2 = new UserEntity(ValidUsername, ValidSub);

        Assert.That(user1.GetHashCode(), Is.Not.EqualTo(user2.GetHashCode()));
    }

    [Test]
    public void GetHashCode_SameUser_ReturnsSameHash()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        int hash1 = user.GetHashCode();
        int hash2 = user.GetHashCode();
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void UpdateBio_DoesNotAffectOtherProperties()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var originalId = user.Id;
        string originalSub = user.Sub;

        user.UpdateBio(new Bio("Some bio."));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Id, Is.EqualTo(originalId));
            Assert.That(user.Sub, Is.EqualTo(originalSub));
        }
    }

    [Test]
    public void UpdateBio_Null_ClearsBio()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        user.UpdateBio(new Bio("Some bio."));

        user.UpdateBio(null);

        Assert.That(user.Bio, Is.Null);
    }

    [Test]
    public void UpdateBio_ValidBio_SetsBio()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var bio = new Bio("I love competitive programming.");

        user.UpdateBio(bio);

        Assert.That(user.Bio, Is.EqualTo(bio));
    }

    [Test]
    public void UpdateImageUrl_Null_ClearsImageUrl()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        user.UpdateImageUrl(new ImageUrl("https://example.com/avatar.png"));

        user.UpdateImageUrl(null);

        Assert.That(user.ImageUrl, Is.Null);
    }

    [Test]
    public void UpdateImageUrl_ValidUrl_SetsImageUrl()
    {
        var user = new UserEntity(ValidUsername, ValidSub);
        var imageUrl = new ImageUrl("https://example.com/avatar.png");

        user.UpdateImageUrl(imageUrl);

        Assert.That(user.ImageUrl, Is.EqualTo(imageUrl));
    }
}