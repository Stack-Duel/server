using StackDuel.Application.Services.Users;
using StackDuel.Domain.Users.Entities;
using StackDuel.Domain.Users.ValueObjects;

namespace StackDuel.Application.Tests.Services.Users;

public class AvatarRetentionPolicyTests
{
    [Test]
    public void SelectStale_FewerThanLimit_ReturnsNone()
    {
        var userId = Guid.NewGuid();
        var current = new UserAvatar(userId, new ImageUrl("https://storage.example.com/1.png"));
        var other = new UserAvatar(userId, new ImageUrl("https://storage.example.com/2.png"));

        IReadOnlyList<UserAvatar> stale = AvatarRetentionPolicy.SelectStale([current, other], current.ImageUrl.Value);

        Assert.That(stale, Is.Empty);
    }

    [Test]
    public void SelectStale_MoreThanLimit_KeepsCurrentAndMostRecentOthers()
    {
        var userId = Guid.NewGuid();
        var newest = new UserAvatar(userId, new ImageUrl("https://storage.example.com/1.png"));
        var second = new UserAvatar(userId, new ImageUrl("https://storage.example.com/2.png"));
        var third = new UserAvatar(userId, new ImageUrl("https://storage.example.com/3.png"));
        var oldest = new UserAvatar(userId, new ImageUrl("https://storage.example.com/4.png"));

        IReadOnlyList<UserAvatar> stale = AvatarRetentionPolicy.SelectStale(
            [newest, second, third, oldest],
            newest.ImageUrl.Value
        );

        Assert.That(stale, Is.EquivalentTo(new[] { oldest }));
    }

    [Test]
    public void SelectStale_CurrentIsOlderThanTheMostRecentOthers_StillKeepsCurrent()
    {
        var userId = Guid.NewGuid();
        var newest = new UserAvatar(userId, new ImageUrl("https://storage.example.com/1.png"));
        var second = new UserAvatar(userId, new ImageUrl("https://storage.example.com/2.png"));
        var third = new UserAvatar(userId, new ImageUrl("https://storage.example.com/3.png"));
        var selectedOlder = new UserAvatar(userId, new ImageUrl("https://storage.example.com/4.png"));

        IReadOnlyList<UserAvatar> stale = AvatarRetentionPolicy.SelectStale(
            [newest, second, third, selectedOlder],
            selectedOlder.ImageUrl.Value
        );

        Assert.That(stale, Is.EquivalentTo(new[] { third }));
    }

    [Test]
    public void SelectStale_NoCurrentImageUrl_KeepsMostRecentOthersOnly()
    {
        var userId = Guid.NewGuid();
        var newest = new UserAvatar(userId, new ImageUrl("https://storage.example.com/1.png"));
        var second = new UserAvatar(userId, new ImageUrl("https://storage.example.com/2.png"));
        var third = new UserAvatar(userId, new ImageUrl("https://storage.example.com/3.png"));

        IReadOnlyList<UserAvatar> stale = AvatarRetentionPolicy.SelectStale([newest, second, third], null);

        Assert.That(stale, Is.EquivalentTo(new[] { third }));
    }
}