using MediatR;
using Microsoft.Extensions.Caching.Memory;
using StackDuel.Application.Queries.FeatureFlags.GetAllFeatureFlagsForViewer;

namespace StackDuel.Application.Services.FeatureFlags;

public interface IFeatureFlagResolutionService
{
    Task<IReadOnlyDictionary<string, bool>> GetFlagsForViewerAsync(
        Guid? userId,
        CancellationToken cancellationToken = default
    );
}

internal sealed class FeatureFlagResolutionService(IMediator mediator, IMemoryCache cache)
    : IFeatureFlagResolutionService
{
    private static readonly IReadOnlyDictionary<string, bool> Empty = new Dictionary<string, bool>();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);

    public async Task<IReadOnlyDictionary<string, bool>> GetFlagsForViewerAsync(
        Guid? userId,
        CancellationToken cancellationToken = default
    )
    {
        string cacheKey = $"feature-flags:{userId?.ToString() ?? "anonymous"}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, bool>? cached) && cached is not null)
            return cached;

        var result = await mediator.Send(new GetAllFeatureFlagsForViewerQuery(userId), cancellationToken);
        IReadOnlyDictionary<string, bool> flags = result.IsSuccess ? result.Value : Empty;

        cache.Set(cacheKey, flags, CacheDuration);

        return flags;
    }
}