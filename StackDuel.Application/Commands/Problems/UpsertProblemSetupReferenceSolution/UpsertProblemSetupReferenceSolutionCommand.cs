using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;

internal sealed record UpsertProblemSetupReferenceSolutionCommand(
    Guid ProblemId,
    Guid LanguageVersionId,
    string InitialCode,
    string? FunctionName,
    string ReferenceSolutionCode
) : ICommand<Guid>, IAuditableCommand
{
    public string AuditAction => "problem.setup-reference-solution-upserted";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => new { LanguageVersionId, FunctionName };
}