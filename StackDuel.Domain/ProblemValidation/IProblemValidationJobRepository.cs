using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.ProblemValidation;

public interface IProblemValidationJobRepository : IRepository<ProblemValidationJob>
{
    Task<IReadOnlyList<ProblemValidationJob>> FindPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<ProblemValidationJob>> FindAwaitingGenerationAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Atomically claims the job for exclusive processing by extending its lock lease, but
    /// only if it isn't already locked by another worker. Works against a job in either
    /// <see cref="StackDuel.Domain.ProblemValidation.Enums.ProblemValidationJobStatus.Pending"/> or
    /// <see cref="StackDuel.Domain.ProblemValidation.Enums.ProblemValidationJobStatus.AwaitingGeneration"/>.
    /// </summary>
    Task<bool> TryClaimAsync(Guid jobId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    /// <summary>Releases the processing lock so the job can be picked up again if still unfinished.</summary>
    Task ReleaseLockAsync(Guid jobId, CancellationToken cancellationToken = default);
}