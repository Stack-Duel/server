using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.StudioProblems.Dtos;

public sealed record StudioProblemDetailsDto(
    Guid ProblemId,
    string Slug,
    string Title,
    int DifficultyValue,
    ProblemStatus Status,
    int TimeLimitMs,
    int MemoryLimitMb,
    int SetupCount,
    DateTime CreatedAt,
    string Question,
    int ReactionCount = 0,
    int AttemptCount = 0,
    double SolveRatePercent = 0,
    DifficultyTier DifficultyTier = DifficultyTier.Beginner
);
