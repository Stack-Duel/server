using StackDuel.Application.FeatureFlags.Dtos;

namespace StackDuel.Application.FeatureFlags;

public interface IFeatureFlagAdminReadRepository
{
    Task<IReadOnlyList<FeatureFlagAdminDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<FeatureFlagAdminDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}