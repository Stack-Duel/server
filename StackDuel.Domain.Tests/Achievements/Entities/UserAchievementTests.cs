using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Achievements.Events;

namespace StackDuel.Domain.Tests.Achievements.Entities;

public class UserAchievementTests
{
    [Fact]
    public void Constructor_SetsUserAndDefinition()
    {
        Guid userId = Guid.NewGuid();
        Guid definitionId = Guid.NewGuid();

        UserAchievement earned = new(userId, definitionId);

        Assert.Equal(userId, earned.UserId);
        Assert.Equal(definitionId, earned.AchievementDefinitionId);
    }

    [Fact]
    public void Constructor_StampsEarnedAt()
    {
        DateTime before = DateTime.UtcNow;

        UserAchievement earned = new(Guid.NewGuid(), Guid.NewGuid());

        Assert.True(earned.EarnedAt >= before);
    }

    [Fact]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievement(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_EmptyDefinitionId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievement(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Constructor_RaisesAchievementUnlockedDomainEvent()
    {
        Guid userId = Guid.NewGuid();
        Guid definitionId = Guid.NewGuid();

        UserAchievement earned = new(userId, definitionId);

        AchievementUnlockedDomainEvent unlocked = earned.DomainEvents.OfType<AchievementUnlockedDomainEvent>().Single();

        Assert.Equal(userId, unlocked.UserId);
        Assert.Equal(definitionId, unlocked.AchievementDefinitionId);
        Assert.Equal(earned.Id, unlocked.UserAchievementId);
        Assert.Equal(earned.EarnedAt, unlocked.EarnedAt);
    }

    [Fact]
    public void PopDomainEvents_DrainsTheUnlockedEvent()
    {
        UserAchievement earned = new(Guid.NewGuid(), Guid.NewGuid());

        Assert.Single(earned.PopDomainEvents());
        Assert.Empty(earned.DomainEvents);
    }
}