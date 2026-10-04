using StackDuel.Application.FeatureFlags;
using StackDuel.Application.FeatureFlags.Dtos;
using StackDuel.Domain.FeatureFlags.ValueObjects;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.FeatureFlags;

internal sealed class FeatureFlagAdminReadRepository(StackDuelDbContext context) : IFeatureFlagAdminReadRepository
{
    public async Task<IReadOnlyList<FeatureFlagAdminDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context
            .FeatureFlags.AsNoTracking()
            .OrderBy(f => f.Key)
            .Select(f => new FeatureFlagAdminDto(
                f.Id.Value,
                f.Key.Value,
                f.Name,
                f.Description,
                f.DefaultEnabled,
                f.RolloutPercentage.Value,
                f.CreatedAt,
                f.UpdatedAt,
                f.UserOverrides.Select(o => new FeatureFlagUserOverrideDto(o.UserId.Value, o.Effect)).ToList(),
                f.GroupOverrides.Select(o => new FeatureFlagGroupOverrideDto(o.GroupId.Value, o.Effect)).ToList()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<FeatureFlagAdminDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        FeatureFlagKey flagKey = new(key);

        return await context
            .FeatureFlags.AsNoTracking()
            .Where(f => f.Key == flagKey)
            .Select(f => new FeatureFlagAdminDto(
                f.Id.Value,
                f.Key.Value,
                f.Name,
                f.Description,
                f.DefaultEnabled,
                f.RolloutPercentage.Value,
                f.CreatedAt,
                f.UpdatedAt,
                f.UserOverrides.Select(o => new FeatureFlagUserOverrideDto(o.UserId.Value, o.Effect)).ToList(),
                f.GroupOverrides.Select(o => new FeatureFlagGroupOverrideDto(o.GroupId.Value, o.Effect)).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}