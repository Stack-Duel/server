using StackDuel.Application.Audit;
using StackDuel.Application.Authorization;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.DeleteProblem;

internal sealed record DeleteProblemCommand(Guid ProblemId, string ConfirmationTitle, Guid? RequestedByUserId = null)
    : ICommand,
        IAuditableCommand,
        INotFoundWhenUnauthorizedCommand
{
    public string AuditAction => "problem.deleted";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => null;

    public IReadOnlyList<IAuthorizationRequirement> Requirements =>
        ProblemCommandAuthorization.OwnershipRequirements(ProblemId, RequestedByUserId);
}