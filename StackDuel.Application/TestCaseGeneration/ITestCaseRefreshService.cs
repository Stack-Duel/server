namespace StackDuel.Application.TestCaseGeneration;

/// <summary>
/// Re-rolls the random hidden test case pool for every eligible problem setup by enqueueing a
/// fresh <see cref="Domain.TestCaseGeneration.Entities.TestCaseGenerationJob"/> per setup — the
/// same queue a seeder uses for a problem's first generation. Enqueuing only: the existing
/// Quartz-driven sweep (<c>TestCaseGenerationJobProcessorJob</c>) does the actual Judge0 work in
/// throttled batches, so this stays cheap and safe to run against an arbitrarily large catalog.
/// </summary>
public interface ITestCaseRefreshService
{
    Task<int> EnqueueRefreshesAsync(CancellationToken cancellationToken = default);
}