using StackDuel.Application.Audit;
using StackDuel.Application.Authorization;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.RemoveProblemSetup;

internal sealed record RemoveProblemSetupCommand(
    Guid ProblemId,
    Guid LanguageVersionId,
    Guid? RequestedByUserId = null
) : ICommand, IAuditableCommand, INotFoundWhenUnauthorizedCommand
{
    public string AuditAction => "problem.setup-removed";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => new { LanguageVersionId };

    public IReadOnlyList<IAuthorizationRequirement> Requirements =>
        ProblemCommandAuthorization.OwnershipRequirements(ProblemId, RequestedByUserId);
}