using StackDuel.Application.Audit;
using StackDuel.Application.Authorization;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.SubmitProblemForValidation;

internal sealed record SubmitProblemForValidationCommand(
    Guid ProblemId,
    Guid? RequestedByUserId = null
) : ICommand, IAuditableCommand, INotFoundWhenUnauthorizedCommand
{
    public string AuditAction => "problem.submitted-for-validation";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => null;

    public IReadOnlyList<IAuthorizationRequirement> Requirements =>
        ProblemCommandAuthorization.OwnershipRequirements(ProblemId, RequestedByUserId);
}
