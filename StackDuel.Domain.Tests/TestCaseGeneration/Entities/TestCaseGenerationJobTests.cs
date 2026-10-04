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

    [Test]
    public void Constructor_ValidInputs_StartsPending()
    {
        var job = CreateJob();

        Assert.Multiple(() =>
        {
            Assert.That(job.Status, Is.EqualTo(TestCaseGenerationJobStatus.Pending));
            Assert.That(job.TargetCaseCount, Is.EqualTo(20));
            Assert.That(job.CompletedAt, Is.Null);
            Assert.That(job.ResultSummary, Is.Null);
            Assert.That(job.FailureReason, Is.Null);
        });
    }

    [Test]
    public void Complete_SetsStatusAndSummary()
    {
        var job = CreateJob();

        job.Complete("Generated 20/20 (0 attempts skipped, 0 warnings).");

        Assert.Multiple(() =>
        {
            Assert.That(job.Status, Is.EqualTo(TestCaseGenerationJobStatus.Completed));
            Assert.That(job.ResultSummary, Is.EqualTo("Generated 20/20 (0 attempts skipped, 0 warnings)."));
            Assert.That(job.CompletedAt, Is.Not.Null);
        });
    }

    [Test]
    public void Fail_SetsStatusAndReason()
    {
        var job = CreateJob();

        job.Fail("Reference solution failed the sanity check.");

        Assert.Multiple(() =>
        {
            Assert.That(job.Status, Is.EqualTo(TestCaseGenerationJobStatus.Failed));
            Assert.That(job.FailureReason, Is.EqualTo("Reference solution failed the sanity check."));
            Assert.That(job.CompletedAt, Is.Not.Null);
        });
    }

    [Test]
    public void Constructor_NoParameters_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TestCaseGenerationJob(Guid.NewGuid(), "code", [], "integer", 20, 42)
        );
    }

    [Test]
    public void Constructor_NonPositiveTargetCaseCount_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TestCaseGenerationJob(Guid.NewGuid(), "code", ValidParameters, "integer", 0, 42)
        );
    }
}