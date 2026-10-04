namespace StackDuel.Application.Problems.Dtos;

public sealed record PublicTestCaseInputDto(string Value, string ValueType);

public sealed record PublicTestCaseExpectedOutputDto(string Value, string ValueType);

public sealed record PublicTestCaseDto(
    string Name,
    string? Description,
    IEnumerable<PublicTestCaseInputDto> Inputs,
    IEnumerable<PublicTestCaseExpectedOutputDto> ExpectedOutputs
);