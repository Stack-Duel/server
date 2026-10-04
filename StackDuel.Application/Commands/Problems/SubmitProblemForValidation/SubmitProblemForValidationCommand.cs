using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.SubmitProblemForValidation;

internal sealed record SubmitProblemForValidationCommand(Guid ProblemId) : ICommand, IAuditableCommand
{
    public string AuditAction => "problem.submitted-for-validation";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => null;
}