using StackDuel.Application.Audit;
using StackDuel.Application.Authorization;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.AddProblemSetup;

internal sealed record AddProblemSetupCommand(
    Guid ProblemId,
    Guid LanguageVersionId,
    Guid? RequestedByUserId = null
) : ICommand<Guid>, IAuditableCommand, INotFoundWhenUnauthorized<Guid>
{
    public string AuditAction => "problem.setup-added";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => new { LanguageVersionId };

    public IReadOnlyList<IAuthorizationRequirement> Requirements =>
        ProblemCommandAuthorization.OwnershipRequirements(ProblemId, RequestedByUserId);
}