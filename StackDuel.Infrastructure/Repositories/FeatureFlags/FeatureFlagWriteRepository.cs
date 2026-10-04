using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.FeatureFlags.ValueObjects;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.FeatureFlags;

internal sealed class FeatureFlagWriteRepository(StackDuelDbContext context) : IFeatureFlagWriteRepository
{
    public async Task<FeatureFlag?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        FeatureFlagId flagId = new(id);

        return await context
            .FeatureFlags.Include(f => f.UserOverrides)
            .Include(f => f.GroupOverrides)
            .FirstOrDefaultAsync(f => f.Id == flagId, cancellationToken);
    }

    public async Task<FeatureFlag?> FindByKeyAsync(string key, CancellationToken cancellationToken)
    {
        FeatureFlagKey flagKey = new(key);

        return await context
            .FeatureFlags.Include(f => f.UserOverrides)
            .Include(f => f.GroupOverrides)
            .FirstOrDefaultAsync(f => f.Key == flagKey, cancellationToken);
    }

    public async Task AddAsync(FeatureFlag flag, CancellationToken cancellationToken) =>
        await context.FeatureFlags.AddAsync(flag, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);
}