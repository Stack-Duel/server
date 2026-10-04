using StackDuel.Domain.SeedWork;
using StackDuel.Domain.SubmissionJobs.Enums;

namespace StackDuel.Domain.SubmissionJobs.Entities;

public sealed class SubmissionJobAttempt : Entity
{
    internal SubmissionJobAttempt(Guid pipelineStepId, int attemptNumber)
    {
        PipelineStepId = pipelineStepId;
        AttemptNumber = attemptNumber;
        Status = SubmissionJobAttemptStatus.Processing;
        StartedAt = DateTime.UtcNow;
    }

    public void Succeed(string? requestPayload, string? responsePayload)
    {
        Status = SubmissionJobAttemptStatus.Succeeded;
        RequestPayload = requestPayload;
        ResponsePayload = responsePayload;
        CompletedAt = DateTime.UtcNow;
        DurationMs = (int)(CompletedAt.Value - StartedAt).TotalMilliseconds;
    }

    public void Fail(string error, string? requestPayload = null, string? responsePayload = null)
    {
        Status = SubmissionJobAttemptStatus.Failed;
        Error = error;
        RequestPayload = requestPayload;
        ResponsePayload = responsePayload;
        CompletedAt = DateTime.UtcNow;
        DurationMs = (int)(CompletedAt.Value - StartedAt).TotalMilliseconds;
    }

    public void Abandon()
    {
        Status = SubmissionJobAttemptStatus.Abandoned;
        CompletedAt = DateTime.UtcNow;
        DurationMs = (int)(CompletedAt.Value - StartedAt).TotalMilliseconds;
    }

    private SubmissionJobAttempt() { }

    public Guid PipelineStepId { get; private set; }
    public int AttemptNumber { get; private set; }
    public SubmissionJobAttemptStatus Status { get; private set; }
    public string? RequestPayload { get; private set; }
    public string? ResponsePayload { get; private set; }
    public string? Error { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int? DurationMs { get; private set; }
}