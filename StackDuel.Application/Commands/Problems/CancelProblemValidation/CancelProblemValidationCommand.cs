using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.CancelProblemValidation;

internal sealed record CancelProblemValidationCommand(Guid ProblemId) : ICommand, IAuditableCommand
{
    public string AuditAction => "problem.validation-cancelled";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => null;
}