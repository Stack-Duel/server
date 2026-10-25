using StackDuel.Application.Audit;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Commands.Problems.CreateDraftProblem;

internal sealed record CreateDraftProblemCommand(CreateDraftProblemInput Input) : ICommand<Guid>, IAuditableCommand
{
    public string AuditAction => "problem.draft-created";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => null;
    public object? AuditDetails => Input;
}
