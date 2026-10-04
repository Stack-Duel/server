using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.Problems.Dtos;

public sealed record AdminProblemDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Question,
    int DifficultyValue,
    DifficultyTier DifficultyTier,
    int TimeLimitMs,
    int MemoryLimitMb,
    ProblemStatus Status,
    string? ValidationFailureReason,
    Guid? TrackId,
    string? TrackName,
    DateTime CreatedAt,
    string? CreatedByUsername,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> PoolKeys,
    IReadOnlyList<AdminProblemSetupDto> Setups,
    IReadOnlyList<GenerationParameterSpec> GenerationParameters,
    string? GenerationOutputValueType,
    int? GenerationTargetCaseCount,
    int? GenerationSeed
);

public sealed record AdminProblemSetupDto(
    Guid Id,
    Guid LanguageVersionId,
    string LanguageName,
    string LanguageVersion,
    string? FunctionName,
    string InitialCode,
    string? ReferenceSolutionCode,
    bool HasReferenceSolution,
    bool HasGenerationSpec,
    int TestSuiteCount,
    int TestCaseCount,
    IReadOnlyList<AdminSampleTestCaseDto> SampleTestCases
);

public sealed record AdminSampleTestCaseDto(
    Guid Id,
    string? Name,
    IReadOnlyList<AdminSampleTestCaseInputDto> Inputs,
    string ExpectedOutputValue,
    string ExpectedOutputValueType
);

public sealed record AdminSampleTestCaseInputDto(string Value, string ValueType);