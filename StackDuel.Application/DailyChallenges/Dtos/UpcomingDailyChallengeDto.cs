using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.DailyChallenges.Dtos;

public sealed record UpcomingDailyChallengeDto(
    DateOnly Date,
    Guid ProblemId,
    string ProblemSlug,
    string ProblemTitle,
    DifficultyTier DifficultyTier,
    ProblemStatus Status
);