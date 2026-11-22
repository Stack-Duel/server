namespace StackDuel.Application.StudioProblems.Dtos;

public sealed record StudioProblemSampleTestCaseInputDto(string Value, string ValueType);

public sealed record StudioProblemSampleTestCaseDto(
    Guid Id,
    string? Name,
    IReadOnlyList<StudioProblemSampleTestCaseInputDto> Inputs,
    string ExpectedOutputValue,
    string ExpectedOutputValueType
);

public sealed record StudioProblemDetailsSetupDto(
    Guid LanguageVersionId,
    string LanguageVersion,
    Guid LanguageId,
    string Language,
    string StarterCode,
    string FunctionName,
    string ReferenceSolution,
    string AmountOfTestSuites,
    string AmountOfTestCases,
    IReadOnlyList<StudioProblemSampleTestCaseDto>? SampleTestCases = null
);