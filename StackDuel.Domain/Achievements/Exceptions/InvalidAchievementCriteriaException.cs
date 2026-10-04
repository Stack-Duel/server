using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Achievements.Exceptions;

public sealed class InvalidAchievementCriteriaException(string reason)
    : DomainException($"Achievement criteria is invalid: {reason}")
{ }