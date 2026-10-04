using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class ProblemPoolRepository(StackDuelDbContext context) : IProblemPoolRepository
{
    public async Task<ProblemPool?> FindByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        await context.ProblemPools.Include("_items").FirstOrDefaultAsync(pool => pool.Key == key, cancellationToken);

    public async Task<bool> ExistsByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        await context.ProblemPools.AnyAsync(pool => pool.Key == key, cancellationToken);

    public async Task<IReadOnlyList<ProblemPoolDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, int> counts = await context
            .Set<ProblemPoolItem>()
            .GroupBy(item => EF.Property<Guid>(item, "pool_id"))
            .Select(group => new { PoolId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.PoolId, x => x.Count, cancellationToken);

        List<ProblemPool> pools = await context
            .ProblemPools.AsNoTracking()
            .OrderBy(pool => pool.Name)
            .ToListAsync(cancellationToken);

        return
        [
            .. pools.Select(pool => new ProblemPoolDto(
                pool.Id,
                pool.Key,
                pool.Name,
                pool.Description,
                counts.TryGetValue(pool.Id, out int count) ? count : 0,
                pool.CreatedAt
            )),
        ];
    }

    public async Task<IReadOnlyList<string>> GetPoolKeysForProblemAsync(
        Guid problemId,
        CancellationToken cancellationToken = default
    )
    {
        List<Guid> poolIds = await context
            .Set<ProblemPoolItem>()
            .Where(item => item.ProblemId == problemId)
            .Select(item => EF.Property<Guid>(item, "pool_id"))
            .ToListAsync(cancellationToken);

        if (poolIds.Count == 0)
            return [];

        return await context
            .ProblemPools.AsNoTracking()
            .Where(pool => poolIds.Contains(pool.Id))
            .OrderBy(pool => pool.Name)
            .Select(pool => pool.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProblemPool pool, CancellationToken cancellationToken = default)
    {
        await context.ProblemPools.AddAsync(pool, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProblemPool pool, CancellationToken cancellationToken = default)
    {
        if (context.Entry(pool).State == EntityState.Detached)
            context.ProblemPools.Update(pool);

        IEnumerable<ProblemPoolItem> items =
            context.Entry(pool).Collection("_items").CurrentValue?.Cast<ProblemPoolItem>() ?? [];
        await NewChildEntityTracker.MarkNewAsAddedAsync(context, items, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}