using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.DailyChallenges.Dtos;

public sealed record TodaysDailyChallengeDto(
    Guid ProblemId,
    string Slug,
    string Title,
    DifficultyTier DifficultyTier,
    bool SolvedByCurrentUser,
    int CurrentStreak,
    int LongestStreak
);