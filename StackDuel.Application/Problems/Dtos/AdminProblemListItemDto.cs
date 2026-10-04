using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Problems.Dtos;

public sealed record AdminProblemListItemDto(
    Guid Id,
    string Slug,
    string Title,
    int DifficultyValue,
    DifficultyTier DifficultyTier,
    ProblemStatus Status,
    int TimeLimitMs,
    int MemoryLimitMb,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Languages,
    int SetupCount,
    DateTime CreatedAt,
    string? CreatedByUsername
);

/// <summary>
/// Repository-level projection before language version ids are resolved to display names.
/// </summary>
public sealed record AdminProblemListRowDto(
    Guid Id,
    string Slug,
    string Title,
    int DifficultyValue,
    ProblemStatus Status,
    int TimeLimitMs,
    int MemoryLimitMb,
    IReadOnlyList<string> Tags,
    IReadOnlyList<Guid> LanguageVersionIds,
    int SetupCount,
    DateTime CreatedAt,
    string? CreatedByUsername
);