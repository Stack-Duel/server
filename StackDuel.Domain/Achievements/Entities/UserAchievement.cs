using StackDuel.Domain.Achievements.Events;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Achievements.Entities;

/// <summary>
/// Records that a user has earned a specific <see cref="AchievementDefinition"/>. Awarding one
/// is the interesting domain event — created once per (user, definition) pair and never mutated.
/// </summary>
public sealed class UserAchievement : AggregateRoot
{
    public UserAchievement(Guid userId, Guid achievementDefinitionId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        if (achievementDefinitionId == Guid.Empty)
            throw new ArgumentException(
                "Achievement definition id must not be empty.",
                nameof(achievementDefinitionId)
            );

        UserId = userId;
        AchievementDefinitionId = achievementDefinitionId;
        EarnedAt = DateTime.UtcNow;

        AddDomainEvent(new AchievementUnlockedDomainEvent(UserId, AchievementDefinitionId, Id, EarnedAt));
    }

    private UserAchievement() { }

    public Guid UserId { get; private set; }

    public Guid AchievementDefinitionId { get; private set; }

    public DateTime EarnedAt { get; private set; }
}