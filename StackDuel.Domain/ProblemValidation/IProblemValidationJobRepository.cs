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

    Task<bool> TryClaimAsync(Guid jobId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    /// Reads the job's cancellation state straight from the database, bypassing anything the
    /// caller's DbContext is already tracking.
    Task<bool?> IsCancelledAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task ReleaseLockAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task SetRetryAfterAsync(Guid jobId, TimeSpan delay, CancellationToken cancellationToken = default);

    Task<ProblemValidationJob?> FindActiveForProblemAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    );
}