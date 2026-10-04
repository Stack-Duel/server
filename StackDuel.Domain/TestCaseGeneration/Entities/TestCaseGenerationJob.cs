using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestCaseGeneration.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Domain.TestCaseGeneration.Entities;

/// <summary>
/// A queued request to (re)generate a problem's random hidden test case pool. Decouples
/// requesting generation (a seeder today, an admin API eventually) from actually running it —
/// the request is just persisted here, and a background processor works through pending jobs
/// on its own schedule, so enqueuing never blocks the caller and many problems can be queued
/// at once without needing Judge0 available at request time.
/// </summary>
public sealed class TestCaseGenerationJob : AggregateRoot
{
    public TestCaseGenerationJob(
        Guid problemSetupId,
        string referenceSolutionCode,
        IReadOnlyList<GenerationParameterSpec> parameters,
        string outputValueType,
        int targetCaseCount,
        int seed
    )
    {
        ProblemSetupId =
            problemSetupId != Guid.Empty
                ? problemSetupId
                : throw new ArgumentException("Problem setup id must not be empty.", nameof(problemSetupId));

        ReferenceSolutionCode = !string.IsNullOrWhiteSpace(referenceSolutionCode)
            ? referenceSolutionCode
            : throw new ArgumentException("Reference solution code must not be empty.", nameof(referenceSolutionCode));

        Parameters = parameters is { Count: > 0 }
            ? parameters
            : throw new ArgumentException("At least one parameter is required.", nameof(parameters));

        OutputValueType = !string.IsNullOrWhiteSpace(outputValueType)
            ? outputValueType
            : throw new ArgumentException("Output value type must not be empty.", nameof(outputValueType));

        TargetCaseCount =
            targetCaseCount > 0
                ? targetCaseCount
                : throw new ArgumentException("Target case count must be greater than zero.", nameof(targetCaseCount));

        Seed = seed;
        Status = TestCaseGenerationJobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    private TestCaseGenerationJob() { }

    public void Complete(string resultSummary)
    {
        Status = TestCaseGenerationJobStatus.Completed;
        ResultSummary = resultSummary;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = TestCaseGenerationJobStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }

    public Guid ProblemSetupId { get; private set; }
    public string ReferenceSolutionCode { get; private set; } = null!;
    public IReadOnlyList<GenerationParameterSpec> Parameters { get; private set; } = [];
    public string OutputValueType { get; private set; } = null!;
    public int TargetCaseCount { get; private set; }
    public int Seed { get; private set; }
    public TestCaseGenerationJobStatus Status { get; private set; }
    public string? ResultSummary { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Lease expiry for the worker currently processing this job — prevents two overlapping
    /// Quartz sweeps (a run that takes longer than the trigger interval) from double-processing
    /// the same job. Set/cleared atomically via ExecuteUpdateAsync, not on the tracked entity.
    /// </summary>
    public DateTime? LockedUntil { get; private set; }
}