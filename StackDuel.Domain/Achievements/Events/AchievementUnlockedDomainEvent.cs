using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Achievements.Events;

public sealed record AchievementUnlockedDomainEvent(
    Guid UserId,
    Guid AchievementDefinitionId,
    Guid UserAchievementId,
    DateTime EarnedAt
) : IDomainEvent;