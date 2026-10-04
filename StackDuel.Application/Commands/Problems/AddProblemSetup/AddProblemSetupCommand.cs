using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.AddProblemSetup;

internal sealed record AddProblemSetupCommand(Guid ProblemId, Guid LanguageVersionId)
    : ICommand<Guid>,
        IAuditableCommand
{
    public string AuditAction => "problem.setup-added";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails => new { LanguageVersionId };
}