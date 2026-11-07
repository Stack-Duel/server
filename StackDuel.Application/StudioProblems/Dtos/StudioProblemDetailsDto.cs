using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.StudioProblems.Dtos;

public sealed record StudioProblemDetailsDto(
    Guid ProblemId,
    Slug Slug,
    string Title,
    int DifficultyValue,
    ProblemStatus Status,
    int TimeLimitMs,
    int MemoryLimitMb,
    int SetupCount,
    DateTime CreatedAt,
    int ReactionCount = 0,
    int AttemptCount = 0,
    double SolveRatePercent = 0
);
