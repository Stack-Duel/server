using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.Domain.Tests.ProblemValidation.Entities;

public class ProblemValidationJobTests
{
    private const int MaxAttempts = 5;

    private static ProblemValidationJob CreateJob() => new(Guid.NewGuid());

    private static ProblemValidationStep StepFor(ProblemValidationJob job, ProblemValidationStepKind kind) =>
        job.Steps.Single(step => step.Kind == kind);

    [Fact]
    public void Constructor_CreatesOnePendingStepPerKindInOrder()
    {
        ProblemValidationJob job = CreateJob();

        Assert.Equal(Enum.GetValues<ProblemValidationStepKind>().Order(), job.Steps.Select(step => step.Kind));
        Assert.All(job.Steps, step => Assert.Equal(ProblemValidationStepStatus.Pending, step.Status));
    }

    [Fact]
    public void Cancel_CancelsTheRunningStepAndSkipsTheRest()
    {
        ProblemValidationJob job = CreateJob();
        job.SucceedStep(ProblemValidationStepKind.AuthorChecks);
        job.BeginStep(ProblemValidationStepKind.ReferenceSolutions);

        job.Cancel();

        Assert.Equal(ProblemValidationJobStatus.Cancelled, job.Status);
        Assert.True(job.IsCancelled);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal(
            ProblemValidationStepStatus.Succeeded,
            StepFor(job, ProblemValidationStepKind.AuthorChecks).Status
        );
        Assert.Equal(
            ProblemValidationStepStatus.Cancelled,
            StepFor(job, ProblemValidationStepKind.ReferenceSolutions).Status
        );
        Assert.Equal(
            ProblemValidationStepStatus.Skipped,
            StepFor(job, ProblemValidationStepKind.TestCaseGeneration).Status
        );
    }

    [Fact]
    public void Cancel_AfterCompletion_LeavesTheJobCompleted()
    {
        ProblemValidationJob job = CreateJob();
        job.Complete("Published");

        job.Cancel();

        Assert.Equal(ProblemValidationJobStatus.Completed, job.Status);
    }

    [Fact]
    public void Cancel_AfterFailure_LeavesTheJobFailed()
    {
        ProblemValidationJob job = CreateJob();
        job.Fail("Reference solution failed");

        job.Cancel();

        Assert.Equal(ProblemValidationJobStatus.Failed, job.Status);
    }

    [Fact]
    public void RecordAttemptFailure_BelowTheLimit_KeepsTheJobRunnable()
    {
        ProblemValidationJob job = CreateJob();

        job.RecordAttemptFailure("Judge0 unreachable", MaxAttempts);

        Assert.Equal(1, job.ProcessingAttempts);
        Assert.Equal("Judge0 unreachable", job.LastError);
        Assert.Equal(ProblemValidationJobStatus.Pending, job.Status);
    }

    [Fact]
    public void RecordAttemptFailure_AtTheLimit_FailsTheJob()
    {
        ProblemValidationJob job = CreateJob();

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
            job.RecordAttemptFailure("Judge0 unreachable", MaxAttempts);

        Assert.Equal(MaxAttempts, job.ProcessingAttempts);
        Assert.Equal(ProblemValidationJobStatus.Failed, job.Status);
        Assert.Contains("after 5 attempts", job.FailureReason);
    }

    [Fact]
    public void RecordAttemptFailure_OnACancelledJob_IsIgnored()
    {
        ProblemValidationJob job = CreateJob();
        job.Cancel();

        job.RecordAttemptFailure("Judge0 unreachable", MaxAttempts);

        Assert.Equal(0, job.ProcessingAttempts);
        Assert.Equal(ProblemValidationJobStatus.Cancelled, job.Status);
    }

    [Fact]
    public void FailStep_SkipsTheStepsTheRunNeverReached()
    {
        ProblemValidationJob job = CreateJob();
        job.SucceedStep(ProblemValidationStepKind.AuthorChecks);
        job.BeginStep(ProblemValidationStepKind.ReferenceSolutions);

        job.FailStep(ProblemValidationStepKind.ReferenceSolutions, "Python: wrong answer");

        Assert.Equal(
            ProblemValidationStepStatus.Failed,
            StepFor(job, ProblemValidationStepKind.ReferenceSolutions).Status
        );
        Assert.Equal(ProblemValidationStepStatus.Skipped, StepFor(job, ProblemValidationStepKind.Publish).Status);
    }

    [Fact]
    public void Fail_TruncatesAReasonTooLongForTheColumn()
    {
        ProblemValidationJob job = CreateJob();

        job.Fail(new string('x', ProblemValidationJob.MaxFailureReasonLength + 500));

        Assert.Equal(ProblemValidationJob.MaxFailureReasonLength, job.FailureReason!.Length);
    }

    [Fact]
    public void RecordAttemptFailure_TruncatesTheErrorTooLongForTheColumn()
    {
        ProblemValidationJob job = CreateJob();

        job.RecordAttemptFailure(new string('x', ProblemValidationJob.MaxLastErrorLength + 500), MaxAttempts);

        Assert.Equal(ProblemValidationJob.MaxLastErrorLength, job.LastError!.Length);
    }
}