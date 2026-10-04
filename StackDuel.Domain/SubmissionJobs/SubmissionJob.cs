using StackDuel.Domain.SeedWork;
using StackDuel.Domain.SubmissionJobs.Entities;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Domain.SubmissionJobs.Exceptions;

namespace StackDuel.Domain.SubmissionJobs;

public sealed class SubmissionJob : AggregateRoot
{
    public SubmissionJob(Guid submissionId, Guid pipelineId, Guid firstStepId)
    {
        SubmissionId = submissionId;
        PipelineId = pipelineId;
        CurrentStepId = firstStepId;
        Status = SubmissionJobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public SubmissionJobAttempt StartAttempt()
    {
        if (CurrentStepId is null)
            throw new SubmissionJobException("Cannot start attempt: no current step.");

        Status = SubmissionJobStatus.Running;

        int attemptNumber = _attempts.Count(a => a.PipelineStepId == CurrentStepId.Value) + 1;

        var attempt = new SubmissionJobAttempt(CurrentStepId.Value, attemptNumber);
        _attempts.Add(attempt);
        return attempt;
    }

    public void AdvanceTo(Guid nextStepId)
    {
        CurrentStepId = nextStepId;
        Status = SubmissionJobStatus.Pending;
    }

    public void Complete()
    {
        Status = SubmissionJobStatus.Completed;
        CurrentStepId = null;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = SubmissionJobStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }

    public void ResetToPending()
    {
        Status = SubmissionJobStatus.Pending;
    }

    public int AttemptCountForCurrentStep() =>
        CurrentStepId is null ? 0 : _attempts.Count(a => a.PipelineStepId == CurrentStepId.Value);

    private SubmissionJob() { }

    public Guid SubmissionId { get; private set; }
    public Guid PipelineId { get; private set; }
    public Guid? CurrentStepId { get; private set; }
    public SubmissionJobStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Lease expiry for the worker currently processing this job. Prevents the
    /// Quartz sweep and the RabbitMQ-driven continuation flow from processing
    /// the same job concurrently. Set/cleared atomically by the repository via
    /// <c>ExecuteUpdateAsync</c> — not mutated directly on the tracked entity.
    /// </summary>
    public DateTime? LockedUntil { get; private set; }

    public IReadOnlyCollection<SubmissionJobAttempt> Attempts => _attempts.AsReadOnly();

    private readonly List<SubmissionJobAttempt> _attempts = [];
}