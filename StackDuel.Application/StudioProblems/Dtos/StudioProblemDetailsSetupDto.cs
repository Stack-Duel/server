namespace StackDuel.Application.StudioProblems.Dtos;

public sealed record StudioProblemDetailsSetupDto(
    Guid LanguageVersionId,
    string LanguageVersion,
    Guid LanguageId,
    string Language,
    string StarterCode,
    string FunctionName,
    string ReferenceSolution,
    string AmountOfTestSuites,
    string AmountOfTestCases
);
