using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Achievements.Events;

namespace StackDuel.Domain.Tests.Achievements.Entities;

public class UserAchievementTests
{
    [Test]
    public void Constructor_SetsUserAndDefinition()
    {
        Guid userId = Guid.NewGuid();
        Guid definitionId = Guid.NewGuid();

        UserAchievement earned = new(userId, definitionId);

        Assert.Multiple(() =>
        {
            Assert.That(earned.UserId, Is.EqualTo(userId));
            Assert.That(earned.AchievementDefinitionId, Is.EqualTo(definitionId));
        });
    }

    [Test]
    public void Constructor_StampsEarnedAt()
    {
        DateTime before = DateTime.UtcNow;

        UserAchievement earned = new(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(earned.EarnedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievement(Guid.Empty, Guid.NewGuid()));
    }

    [Test]
    public void Constructor_EmptyDefinitionId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievement(Guid.NewGuid(), Guid.Empty));
    }

    [Test]
    public void Constructor_RaisesAchievementUnlockedDomainEvent()
    {
        Guid userId = Guid.NewGuid();
        Guid definitionId = Guid.NewGuid();

        UserAchievement earned = new(userId, definitionId);

        AchievementUnlockedDomainEvent unlocked = earned.DomainEvents.OfType<AchievementUnlockedDomainEvent>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(unlocked.UserId, Is.EqualTo(userId));
            Assert.That(unlocked.AchievementDefinitionId, Is.EqualTo(definitionId));
            Assert.That(unlocked.UserAchievementId, Is.EqualTo(earned.Id));
            Assert.That(unlocked.EarnedAt, Is.EqualTo(earned.EarnedAt));
        });
    }

    [Test]
    public void PopDomainEvents_DrainsTheUnlockedEvent()
    {
        UserAchievement earned = new(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(earned.PopDomainEvents(), Has.Count.EqualTo(1));
        Assert.That(earned.DomainEvents, Is.Empty);
    }
}