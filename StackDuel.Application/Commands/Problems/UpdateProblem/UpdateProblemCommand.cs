using StackDuel.Application.Audit;
using StackDuel.Application.Commands;
using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Commands.Problems.UpdateProblem;

internal sealed record UpdateProblemCommand(
    Guid ProblemId,
    string Title,
    string Question,
    int Difficulty,
    int TimeLimitMs,
    int MemoryLimitMb,
    IReadOnlyCollection<string> Tags,
    ProblemStatus? Status
) : ICommand, IAuditableCommand
{
    public string AuditAction => "problem.updated";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => ProblemId.ToString();
    public object? AuditDetails =>
        new
        {
            Title,
            Difficulty,
            TimeLimitMs,
            MemoryLimitMb,
            Tags,
            Status,
        };
}