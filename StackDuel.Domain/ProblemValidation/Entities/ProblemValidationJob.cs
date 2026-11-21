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

        _steps =
        [
            .. Enum.GetValues<ProblemValidationStepKind>().Order().Select(kind => new ProblemValidationStep(kind)),
        ];
    }

    private ProblemValidationJob() { }

    private readonly List<ProblemValidationStep> _steps = [];

    public IReadOnlyCollection<ProblemValidationStep> Steps => _steps.AsReadOnly();

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

    public void RecordAttemptFailure(string reason, int maxAttempts)
    {
        if (Status is ProblemValidationJobStatus.Completed or ProblemValidationJobStatus.Cancelled)
            return;

        ProcessingAttempts++;
        LastError = Truncate(reason, MaxLastErrorLength);

        if (ProcessingAttempts >= maxAttempts)
            Fail($"Validation could not be completed after {ProcessingAttempts} attempts: {reason}");
    }

    public void Fail(string reason)
    {
        Status = ProblemValidationJobStatus.Failed;
        FailureReason = Truncate(reason, MaxFailureReasonLength);
        CompletedAt = DateTime.UtcNow;

        foreach (ProblemValidationStep step in _steps.Where(step => step.Status == ProblemValidationStepStatus.Running))
            step.Fail(reason, null);

        foreach (ProblemValidationStep step in _steps.Where(step => step.Status == ProblemValidationStepStatus.Pending))
            step.Skip();
    }

    public const int MaxFailureReasonLength = 4000;
    public const int MaxLastErrorLength = 2000;

    private static string? Truncate(string? value, int maxLength) =>
        value is not null && value.Length > maxLength ? value[..maxLength] : value;

    public Guid ProblemId { get; private set; }
    public ProblemValidationJobStatus Status { get; private set; }
    public IReadOnlyList<Guid> TestCaseGenerationJobIds { get; private set; } = [];
    public string? ResultSummary { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public int ProcessingAttempts { get; private set; }
    public string? LastError { get; private set; }

    public DateTime? LockedUntil { get; private set; }
}