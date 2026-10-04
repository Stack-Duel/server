using StackDuel.Application.Audit;
using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Problems.CreateProblemDraft;

internal sealed record CreateProblemDraftCommand(
    string Title,
    string Question,
    int Difficulty,
    int TimeLimitMs,
    int MemoryLimitMb,
    IReadOnlyCollection<string> Tags,
    Guid TrackId
) : ICommand<Guid>, IAuditableCommand
{
    public string AuditAction => "problem.draft-created";
    public string? AuditTargetType => "problem";
    public string? AuditTargetId => null;
    public object? AuditDetails =>
        new
        {
            Title,
            Difficulty,
            TimeLimitMs,
            MemoryLimitMb,
            Tags,
            TrackId,
        };
}