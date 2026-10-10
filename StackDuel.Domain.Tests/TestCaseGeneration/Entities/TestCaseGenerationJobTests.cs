using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Domain.Tests.TestCaseGeneration.Entities;

public class TestCaseGenerationJobTests
{
    private static readonly GenerationParameterSpec[] ValidParameters =
    [
        new("n", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
    ];

    private static TestCaseGenerationJob CreateJob() =>
        new(Guid.NewGuid(), "def f(n): return n", ValidParameters, "integer", targetCaseCount: 20, seed: 42);

    [Fact]
    public void Constructor_ValidInputs_StartsPending()
    {
        var job = CreateJob();

        Assert.Equal(TestCaseGenerationJobStatus.Pending, job.Status);
        Assert.Equal(20, job.TargetCaseCount);
        Assert.Null(job.CompletedAt);
        Assert.Null(job.ResultSummary);
        Assert.Null(job.FailureReason);
    }

    [Fact]
    public void Complete_SetsStatusAndSummary()
    {
        var job = CreateJob();

        job.Complete("Generated 20/20 (0 attempts skipped, 0 warnings).");

        Assert.Equal(TestCaseGenerationJobStatus.Completed, job.Status);
        Assert.Equal("Generated 20/20 (0 attempts skipped, 0 warnings).", job.ResultSummary);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public void Fail_SetsStatusAndReason()
    {
        var job = CreateJob();

        job.Fail("Reference solution failed the sanity check.");

        Assert.Equal(TestCaseGenerationJobStatus.Failed, job.Status);
        Assert.Equal("Reference solution failed the sanity check.", job.FailureReason);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public void Constructor_NoParameters_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TestCaseGenerationJob(Guid.NewGuid(), "code", [], "integer", 20, 42)
        );
    }

    [Fact]
    public void Constructor_NonPositiveTargetCaseCount_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TestCaseGenerationJob(Guid.NewGuid(), "code", ValidParameters, "integer", 0, 42)
        );
    }
}