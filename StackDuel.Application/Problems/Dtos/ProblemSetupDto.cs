namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemSetupTestCaseDto(string Inputs, string ExpectedOutput);

public sealed record ProblemSetupFileDto(string Path, string Content);

public sealed record ProblemSetupDto(
    Guid Id,
    string InitialCode,
    string? FunctionName,
    IEnumerable<ProblemSetupTestCaseDto> TestCases,
    IEnumerable<ProblemSetupFileDto> AdditionalFiles
);