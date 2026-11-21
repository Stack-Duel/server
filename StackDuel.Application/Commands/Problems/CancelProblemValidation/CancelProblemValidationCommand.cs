using StackDuel.Application.Audit;
using StackDuel.Application.Authorization;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.CancelProblemValidation;

internal sealed record CancelProblemValidationCommand(Guid ProblemId, Guid? RequestedByUserId = null)
    : ICommand,
        IAuditableCommand,
        INotFoundWhenUnauthorizedCommand
{
    public string AuditAction => "problem.validation-cancelled";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => null;

    public IReadOnlyList<IAuthorizationRequirement> Requirements =>
        ProblemCommandAuthorization.OwnershipRequirements(ProblemId, RequestedByUserId);
}