using StackDuel.Domain.FeatureFlags.Entities;

namespace StackDuel.Domain.FeatureFlags;

public interface IFeatureFlagWriteRepository
{
    Task<FeatureFlag?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<FeatureFlag?> FindByKeyAsync(string key, CancellationToken cancellationToken);

    Task AddAsync(FeatureFlag flag, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}