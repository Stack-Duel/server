using StackDuel.Domain.SeedWork;
using StackDuel.Domain.SubmissionJobs.Entities;

namespace StackDuel.Domain.SubmissionJobs;

public interface ISubmissionJobRepository : IRepository<SubmissionJob>
{
    Task<IReadOnlyList<SubmissionJob>> FindPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<SubmissionJob?> FindBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims the job for exclusive processing by extending its lock lease,
    /// but only if it isn't already locked by another worker. Prevents the Quartz sweep
    /// and the RabbitMQ-driven continuation flow from double-processing the same job.
    /// Returns <c>false</c> if the job is currently locked, completed, or failed.
    /// </summary>
    Task<bool> TryClaimAsync(Guid jobId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the processing lock so the job can be picked up again (e.g. by the
    /// next continuation message or the sweep, if it's still pending/running).
    /// </summary>
    Task ReleaseLockAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately inserts a newly created attempt into the database.
    /// Call this right after <see cref="SubmissionJob.StartAttempt"/> so the row
    /// exists before the step handler runs, making the final <see cref="UpdateAsync"/>
    /// a pure UPDATE with no INSERT required.
    /// </summary>
    Task PersistAttemptAsync(
        SubmissionJob job,
        SubmissionJobAttempt attempt,
        CancellationToken cancellationToken = default
    );
}