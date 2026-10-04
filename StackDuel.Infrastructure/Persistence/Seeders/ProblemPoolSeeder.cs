using StackDuel.Domain.Problems.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class ProblemPoolSeeder(StackDuelDbContext context) : IStaticSeeder
{
    public const string AllProblemsKey = "all-problems";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        ProblemPool? pool = await context.ProblemPools.FirstOrDefaultAsync(
            p => p.Key == AllProblemsKey,
            cancellationToken
        );

        if (pool is null)
        {
            pool = new ProblemPool(
                AllProblemsKey,
                "All Problems",
                "Every published problem. Used as the default pool for built-in game modes."
            );
            await context.ProblemPools.AddAsync(pool, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        List<Guid> problemIds = await context.Problems.Select(p => p.Id).ToListAsync(cancellationToken);

        List<Guid> existingMemberIds = await context
            .Set<ProblemPoolItem>()
            .Where(item => EF.Property<Guid>(item, "pool_id") == pool.Id)
            .Select(item => item.ProblemId)
            .ToListAsync(cancellationToken);

        HashSet<Guid> existingMemberIdSet = [.. existingMemberIds];
        List<Guid> missingIds = [.. problemIds.Where(id => !existingMemberIdSet.Contains(id))];

        if (missingIds.Count == 0)
            return;

        ProblemPool trackedPool = await context
            .ProblemPools.Include("_items")
            .SingleAsync(p => p.Id == pool.Id, cancellationToken);

        foreach (Guid problemId in missingIds)
            trackedPool.AddProblem(problemId);

        // ProblemPoolItem has a client-generated (non-default) Guid key, so EF's graph-fixup
        // can't tell these newly-added items apart from pre-existing ones reached via the
        // "_items" navigation and marks them Modified instead of Added. Force the ones we just
        // created to Added so SaveChanges inserts them instead of issuing no-op updates.
        foreach (EntityEntry<ProblemPoolItem> entry in context.ChangeTracker.Entries<ProblemPoolItem>())
        {
            if (entry.State == EntityState.Modified)
                entry.State = EntityState.Added;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}