using StackDuel.Domain.SeedWork;
using StackDuel.Domain.TestCaseGeneration.Entities;

namespace StackDuel.Domain.TestCaseGeneration;

public interface ITestCaseGenerationJobRepository : IRepository<TestCaseGenerationJob>
{
    Task<IReadOnlyList<TestCaseGenerationJob>> FindPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Atomically claims the job for exclusive processing by extending its lock lease, but
    /// only if it isn't already locked by another worker. Returns <c>false</c> if the job is
    /// currently locked, completed, or failed.
    /// </summary>
    Task<bool> TryClaimAsync(Guid jobId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    /// <summary>Releases the processing lock so the job can be picked up again if still pending.</summary>
    Task ReleaseLockAsync(Guid jobId, CancellationToken cancellationToken = default);
}