using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemWithSetupsDto(
    Guid Id,
    string Slug,
    string Title,
    DifficultyTier DifficultyTier,
    string Question,
    IEnumerable<ProblemSetupLanguageDto> AvailableLanguages,
    IEnumerable<PublicTestCaseDto> PublicTestCases,
    ProblemAuthorDto? Author,
    IEnumerable<string> Tags
);