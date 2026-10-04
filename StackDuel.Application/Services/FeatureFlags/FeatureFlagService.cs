using StackDuel.Application.Queries.FeatureFlags.GetFeatureFlagEnabled;
using MediatR;

namespace StackDuel.Application.Services.FeatureFlags;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string flagKey, Guid? userId, CancellationToken cancellationToken = default);
}

internal sealed class FeatureFlagService(IMediator mediator) : IFeatureFlagService
{
    public async Task<bool> IsEnabledAsync(string flagKey, Guid? userId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFeatureFlagEnabledQuery(flagKey, userId), cancellationToken);

        return result.IsSuccess && result.Value;
    }
}