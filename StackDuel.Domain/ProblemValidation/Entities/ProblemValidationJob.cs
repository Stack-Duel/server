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

        _steps =
        [
            .. Enum.GetValues<ProblemValidationStepKind>().Order().Select(kind => new ProblemValidationStep(kind)),
        ];
    }

    private ProblemValidationJob() { }

    private readonly List<ProblemValidationStep> _steps = [];

    public IReadOnlyCollection<ProblemValidationStep> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Marks a stage as started. Safe to call again for the same stage: a job can be picked up by
    /// another worker after a lease expires, and re-running a pass should not reset its clock.
    /// </summary>
    public void BeginStep(ProblemValidationStepKind kind) => FindStep(kind)?.Begin();

    public void SucceedStep(
        ProblemValidationStepKind kind,
        string? detail = null,
        IReadOnlyList<ProblemValidationStepItemSpec>? items = null
    ) => FindStep(kind)?.Succeed(detail, items);

    public void FailStep(
        ProblemValidationStepKind kind,
        string? detail = null,
        IReadOnlyList<ProblemValidationStepItemSpec>? items = null
    )
    {
        FindStep(kind)?.Fail(detail, items);
        SkipUnreachedSteps(kind);
    }

    private ProblemValidationStep? FindStep(ProblemValidationStepKind kind) =>
        _steps.FirstOrDefault(step => step.Kind == kind);

    private void SkipUnreachedSteps(ProblemValidationStepKind failedKind)
    {
        foreach (ProblemValidationStep step in _steps.Where(step => step.Kind > failedKind))
            step.Skip();
    }

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

        SucceedStep(ProblemValidationStepKind.Publish, summary);
    }

    /// <summary>
    /// Stops a run the author no longer wants. Cooperative: a worker part-way through a pass sees
    /// the changed status before it writes its outcome and abandons its work rather than publishing.
    /// </summary>
    public void Cancel()
    {
        if (Status is ProblemValidationJobStatus.Completed or ProblemValidationJobStatus.Failed)
            return;

        Status = ProblemValidationJobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;

        foreach (ProblemValidationStep step in _steps.Where(step => step.Status == ProblemValidationStepStatus.Running))
            step.MarkCancelled();

        foreach (ProblemValidationStep step in _steps.Where(step => step.Status == ProblemValidationStepStatus.Pending))
            step.Skip();
    }

    public bool IsCancelled => Status == ProblemValidationJobStatus.Cancelled;

    public void Fail(string reason)
    {
        Status = ProblemValidationJobStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;

        foreach (ProblemValidationStep step in _steps.Where(step => step.Status == ProblemValidationStepStatus.Running))
            step.Fail(reason, null);

        foreach (ProblemValidationStep step in _steps.Where(step => step.Status == ProblemValidationStepStatus.Pending))
            step.Skip();
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