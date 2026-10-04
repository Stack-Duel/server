using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;

public sealed record SampleTestCaseInputDto(string Value, string ValueType);

public sealed record SampleTestCaseDto(
    string? Name,
    IReadOnlyList<SampleTestCaseInputDto> Inputs,
    string ExpectedOutputValue,
    string ExpectedOutputValueType
);

internal sealed record SetProblemSampleTestCasesCommand(Guid ProblemId, IReadOnlyList<SampleTestCaseDto> TestCases)
    : ICommand,
        IAuditableCommand
{
    public string AuditAction => "problem.sample-test-cases-set";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => new { TestCaseCount = TestCases.Count };
}