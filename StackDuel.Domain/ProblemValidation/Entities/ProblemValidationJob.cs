using StackDuel.Domain.ProblemValidation.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ProblemValidation.Entities;

/// <summary>
/// A queued request to validate a newly-authored problem's reference solutions and, on success,
/// generate its hidden test cases before publishing it. Runs in two passes, tracked by
/// <see cref="Status"/>: reference-solution validation, then waiting on the
/// TestCaseGenerationJobs it enqueues. See <see cref="StackDuel.Domain.TestCaseGeneration.Entities.TestCaseGenerationJob"/>
/// for the equivalent single-pass job this one chains into.
/// </summary>
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

    public Guid ProblemId { get; private set; }
    public ProblemValidationJobStatus Status { get; private set; }
    public IReadOnlyList<Guid> TestCaseGenerationJobIds { get; private set; } = [];
    public string? ResultSummary { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Lease expiry for the worker currently processing this job — same purpose as
    /// <see cref="StackDuel.Domain.TestCaseGeneration.Entities.TestCaseGenerationJob.LockedUntil"/>.
    /// Set/cleared atomically via ExecuteUpdateAsync, not on the tracked entity.
    /// </summary>
    public DateTime? LockedUntil { get; private set; }
}