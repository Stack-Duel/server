using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.TestCaseGeneration;

internal sealed class TestCaseGenerationJobRepository(StackDuelDbContext context) : ITestCaseGenerationJobRepository
{
    public async Task AddAsync(TestCaseGenerationJob job, CancellationToken cancellationToken = default)
    {
        await context.TestCaseGenerationJobs.AddAsync(job, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TestCaseGenerationJob job, CancellationToken cancellationToken = default)
    {
        context.TestCaseGenerationJobs.Update(job);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TestCaseGenerationJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.TestCaseGenerationJobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TestCaseGenerationJob>> FindPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;

        return await context
            .TestCaseGenerationJobs.Where(j =>
                j.Status == TestCaseGenerationJobStatus.Pending && (j.LockedUntil == null || j.LockedUntil < now)
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
            .TestCaseGenerationJobs.Where(j =>
                j.Id == jobId
                && j.Status == TestCaseGenerationJobStatus.Pending
                && (j.LockedUntil == null || j.LockedUntil < now)
            )
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LockedUntil, lockedUntil), cancellationToken);

        return rows == 1;
    }

    public async Task ReleaseLockAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await context
            .TestCaseGenerationJobs.Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LockedUntil, (DateTime?)null), cancellationToken);
    }
}