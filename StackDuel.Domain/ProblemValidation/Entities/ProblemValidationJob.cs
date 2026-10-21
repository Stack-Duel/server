using StackDuel.Domain.ProblemValidation.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ProblemValidation.Entities;

public sealed class ProblemValidationJob : AggregateRoot
{
    public ProblemValidationJob(Guid problemId)
    {
        ProblemId =
            problemId != Guid.Empty
                ? problemId
                : throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        Status = ProblemValidationJobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    private ProblemValidationJob() { }

    public void MoveToAwaitingGeneration(IReadOnlyList<Guid> testCaseGenerationJobIds)
    {
        TestCaseGenerationJobIds = testCaseGenerationJobIds;
        Status = ProblemValidationJobStatus.AwaitingGeneration;
    }

    public void Complete(string summary)
    {
        Status = ProblemValidationJobStatus.Completed;
        ResultSummary = summary;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = ProblemValidationJobStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        Status = ProblemValidationJobStatus.Cancelled;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }

    public Guid ProblemId { get; private set; }
    public ProblemValidationJobStatus Status { get; private set; }
    public IReadOnlyList<Guid> TestCaseGenerationJobIds { get; private set; } = [];
    public string? ResultSummary { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public DateTime? LockedUntil { get; private set; }
}