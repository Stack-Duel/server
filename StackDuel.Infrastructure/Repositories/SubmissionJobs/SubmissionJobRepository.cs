using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.SubmissionJobs.Entities;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.SubmissionJobs;

internal sealed class SubmissionJobRepository(StackDuelDbContext context) : ISubmissionJobRepository
{
    public async Task AddAsync(SubmissionJob entity, CancellationToken cancellationToken = default)
    {
        await context.SubmissionJobs.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubmissionJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.SubmissionJobs.Include(j => j.Attempts).FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SubmissionJob>> FindPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;

        // Excludes jobs currently held by another worker's lock lease so the
        // Quartz sweep only ever picks up genuinely idle/orphaned jobs, not
        // ones actively being driven by the RabbitMQ continuation flow.
        return await context
            .SubmissionJobs.Include(j => j.Attempts)
            .Where(j =>
                (j.Status == SubmissionJobStatus.Pending || j.Status == SubmissionJobStatus.Running)
                && (j.LockedUntil == null || j.LockedUntil < now)
            )
            .OrderBy(j => j.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimAsync(
        Guid jobId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        var lockedUntil = now.Add(leaseDuration);

        int rows = await context
            .SubmissionJobs.Where(j =>
                j.Id == jobId
                && (j.Status == SubmissionJobStatus.Pending || j.Status == SubmissionJobStatus.Running)
                && (j.LockedUntil == null || j.LockedUntil < now)
            )
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LockedUntil, lockedUntil), cancellationToken);

        return rows == 1;
    }

    public async Task ReleaseLockAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await context
            .SubmissionJobs.Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LockedUntil, (DateTime?)null), cancellationToken);
    }

    public async Task<SubmissionJob?> FindBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .SubmissionJobs.Include(j => j.Attempts)
            .FirstOrDefaultAsync(j => j.SubmissionId == submissionId, cancellationToken);

    public async Task UpdateAsync(SubmissionJob entity, CancellationToken cancellationToken = default)
    {
        // The attempt was already INSERT-ed by PersistAttemptAsync before the handler ran.
        // All entities are tracked; SaveChangesAsync detects property changes via snapshot
        // and issues only UPDATEs — no INSERT needed here.
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task PersistAttemptAsync(
        SubmissionJob job,
        SubmissionJobAttempt attempt,
        CancellationToken cancellationToken = default
    )
    {
        // Explicitly track the new attempt as Added and wire up the shadow FK,
        // then flush it to the DB immediately. This guarantees the row exists
        // before the step handler runs so that UpdateAsync only ever does UPDATEs.
        var entry = context.Entry(attempt);
        entry.State = EntityState.Added;
        entry.Property("job_id").CurrentValue = job.Id;
        await context.SaveChangesAsync(cancellationToken);
    }
}