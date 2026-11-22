using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.Domain.Tests.ProblemValidation.Entities;

public class ProblemValidationStepTests
{
    private const ProblemValidationStepKind Kind = ProblemValidationStepKind.ReferenceSolutions;

    private static ProblemValidationJob CreateJob() => new(Guid.NewGuid());

    private static ProblemValidationStep StepFor(ProblemValidationJob job) =>
        job.Steps.Single(step => step.Kind == Kind);

    [Fact]
    public void ANewStep_StartsPendingWithNoItemsOrTimestamps()
    {
        ProblemValidationStep step = StepFor(CreateJob());

        Assert.Equal(ProblemValidationStepStatus.Pending, step.Status);
        Assert.Empty(step.Items);
        Assert.Null(step.StartedAt);
        Assert.Null(step.CompletedAt);
        Assert.Null(step.Detail);
    }

    [Fact]
    public void BeginningAStep_MarksItRunningAndStamsTheStartTime()
    {
        ProblemValidationJob job = CreateJob();
        job.BeginStep(Kind);

        ProblemValidationStep step = StepFor(job);

        Assert.Equal(ProblemValidationStepStatus.Running, step.Status);
        Assert.NotNull(step.StartedAt);
    }

    [Fact]
    public void BeginningAnAlreadyFinishedStep_DoesNotReopenIt()
    {
        ProblemValidationJob job = CreateJob();
        job.SucceedStep(Kind, "done");
        DateTime? startedAt = StepFor(job).StartedAt;

        job.BeginStep(Kind);

        ProblemValidationStep step = StepFor(job);
        Assert.Equal(ProblemValidationStepStatus.Succeeded, step.Status);
        Assert.Equal(startedAt, step.StartedAt);
    }

    [Fact]
    public void SucceedingAStepThatNeverBegan_BackfillsTheStartTime()
    {
        ProblemValidationJob job = CreateJob();

        job.SucceedStep(Kind, "done");

        ProblemValidationStep step = StepFor(job);
        Assert.NotNull(step.StartedAt);
        Assert.NotNull(step.CompletedAt);
        Assert.Equal("done", step.Detail);
    }

    [Fact]
    public void SucceedingAStep_RecordsOneItemPerSpec()
    {
        ProblemValidationJob job = CreateJob();

        job.SucceedStep(
            Kind,
            "2 of 2 solutions passed",
            [
                new ProblemValidationStepItemSpec("javascript", ProblemValidationStepStatus.Succeeded, "20/20"),
                new ProblemValidationStepItemSpec("python", ProblemValidationStepStatus.Succeeded),
            ]
        );

        ProblemValidationStep step = StepFor(job);
        Assert.Equal(["javascript", "python"], step.Items.Select(item => item.Label));
        Assert.All(step.Items, item => Assert.Equal(ProblemValidationStepStatus.Succeeded, item.Status));
        Assert.Equal("20/20", step.Items.First().Detail);
        Assert.Null(step.Items.Last().Detail);
    }

    [Fact]
    public void FailingAStep_RecordsTheFailingItems()
    {
        ProblemValidationJob job = CreateJob();

        job.FailStep(
            Kind,
            "1 of 2 solutions failed",
            [
                new ProblemValidationStepItemSpec("javascript", ProblemValidationStepStatus.Succeeded),
                new ProblemValidationStepItemSpec("python", ProblemValidationStepStatus.Failed, "wrong answer"),
            ]
        );

        ProblemValidationStep step = StepFor(job);
        Assert.Equal(ProblemValidationStepStatus.Failed, step.Status);
        Assert.Equal(
            ProblemValidationStepStatus.Failed,
            step.Items.Single(item => item.Label == "python").Status
        );
        Assert.Equal("wrong answer", step.Items.Single(item => item.Label == "python").Detail);
    }

    [Fact]
    public void FinishingAStepTwice_ReplacesTheEarlierItemsRatherThanAppending()
    {
        ProblemValidationJob job = CreateJob();
        job.SucceedStep(Kind, "first", [new ProblemValidationStepItemSpec("stale", ProblemValidationStepStatus.Succeeded)]);

        job.SucceedStep(Kind, "second", [new ProblemValidationStepItemSpec("fresh", ProblemValidationStepStatus.Succeeded)]);

        ProblemValidationStep step = StepFor(job);
        Assert.Equal("fresh", Assert.Single(step.Items).Label);
        Assert.Equal("second", step.Detail);
    }

    [Fact]
    public void FinishingAStepWithoutItems_LeavesTheRecordedItemsAlone()
    {
        ProblemValidationJob job = CreateJob();
        job.SucceedStep(Kind, "first", [new ProblemValidationStepItemSpec("kept", ProblemValidationStepStatus.Succeeded)]);

        job.SucceedStep(Kind, "second", items: null);

        Assert.Equal("kept", Assert.Single(StepFor(job).Items).Label);
    }

    [Fact]
    public void AStepDetailTooLongForTheColumn_IsTruncated()
    {
        ProblemValidationJob job = CreateJob();

        job.FailStep(Kind, new string('x', 2500));

        Assert.Equal(2000, StepFor(job).Detail!.Length);
    }

    [Fact]
    public void AnItemDetailTooLongForTheColumn_IsTruncated()
    {
        ProblemValidationJob job = CreateJob();

        job.FailStep(
            Kind,
            "failed",
            [new ProblemValidationStepItemSpec("javascript", ProblemValidationStepStatus.Failed, new string('x', 2500))]
        );

        Assert.Equal(2000, Assert.Single(StepFor(job).Items).Detail!.Length);
    }

    [Fact]
    public void AnItemWithABlankLabel_IsRejected()
    {
        ProblemValidationJob job = CreateJob();

        Assert.Throws<ArgumentException>(() =>
            job.FailStep(Kind, "failed", [new ProblemValidationStepItemSpec(" ", ProblemValidationStepStatus.Failed)])
        );
    }

    [Fact]
    public void SkippingAStepThatAlreadyRan_LeavesItsOutcomeIntact()
    {
        ProblemValidationJob job = CreateJob();
        job.SucceedStep(ProblemValidationStepKind.AuthorChecks, "done");

        job.FailStep(ProblemValidationStepKind.AuthorChecks, "later failure");

        Assert.Equal(
            ProblemValidationStepStatus.Failed,
            job.Steps.Single(step => step.Kind == ProblemValidationStepKind.AuthorChecks).Status
        );
    }

    [Fact]
    public void AnItemSpec_DefaultsItsDetailToNull()
    {
        ProblemValidationStepItemSpec spec = new("javascript", ProblemValidationStepStatus.Succeeded);

        Assert.Equal("javascript", spec.Label);
        Assert.Equal(ProblemValidationStepStatus.Succeeded, spec.Status);
        Assert.Null(spec.Detail);
    }
}