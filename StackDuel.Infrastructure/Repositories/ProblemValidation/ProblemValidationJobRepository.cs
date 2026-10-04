using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.ProblemValidation;

internal sealed class ProblemValidationJobRepository(StackDuelDbContext context) : IProblemValidationJobRepository
{
    public async Task AddAsync(ProblemValidationJob job, CancellationToken cancellationToken = default)
    {
        await context.ProblemValidationJobs.AddAsync(job, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProblemValidationJob job, CancellationToken cancellationToken = default)
    {
        context.ProblemValidationJobs.Update(job);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProblemValidationJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.ProblemValidationJobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProblemValidationJob>> FindPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;

        return await context
            .ProblemValidationJobs.Where(j =>
                j.Status == ProblemValidationJobStatus.Pending && (j.LockedUntil == null || j.LockedUntil < now)
            )
            .OrderBy(j => j.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProblemValidationJob>> FindAwaitingGenerationAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;

        return await context
            .ProblemValidationJobs.Where(j =>
                j.Status == ProblemValidationJobStatus.AwaitingGeneration
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
            .ProblemValidationJobs.Where(j =>
                j.Id == jobId
                && (
                    j.Status == ProblemValidationJobStatus.Pending
                    || j.Status == ProblemValidationJobStatus.AwaitingGeneration
                )
                && (j.LockedUntil == null || j.LockedUntil < now)
            )
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LockedUntil, lockedUntil), cancellationToken);

        return rows == 1;
    }

    public async Task ReleaseLockAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await context
            .ProblemValidationJobs.Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LockedUntil, (DateTime?)null), cancellationToken);
    }
}