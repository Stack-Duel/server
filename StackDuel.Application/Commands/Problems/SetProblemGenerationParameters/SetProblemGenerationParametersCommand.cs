using StackDuel.Application.Audit;
using StackDuel.Application.Commands;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;

internal sealed record SetProblemGenerationParametersCommand(
    Guid ProblemId,
    IReadOnlyList<GenerationParameterSpec> Parameters,
    string OutputValueType,
    int TargetCaseCount,
    int Seed
) : ICommand, IAuditableCommand
{
    public string AuditAction => "problem.generation-parameters-set";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails =>
        new
        {
            OutputValueType,
            TargetCaseCount,
            Seed,
            ParameterCount = Parameters.Count,
        };
}