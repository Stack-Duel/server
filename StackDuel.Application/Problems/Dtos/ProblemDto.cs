using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemLanguageDto(Guid Id, string Name, string Slug);

public sealed record ProblemDto(
    Guid Id,
    string Slug,
    string Title,
    DifficultyTier DifficultyTier,
    IReadOnlyList<string> Tags,
    IReadOnlyList<ProblemLanguageDto> Languages
);

/// <summary>
/// Repository-level projection before language version ids are resolved to language display info.
/// </summary>
public sealed record ProblemListRowDto(
    Guid Id,
    string Slug,
    string Title,
    int DifficultyValue,
    IReadOnlyList<string> Tags,
    IReadOnlyList<Guid> LanguageVersionIds
);