using Ardalis.Result;
using StackDuel.Application.FeatureFlags;
using StackDuel.Application.FeatureFlags.Dtos;

namespace StackDuel.Application.Queries.FeatureFlags.GetFeatureFlags;

internal sealed class GetFeatureFlagsHandler(IFeatureFlagAdminReadRepository adminReadRepository)
    : IQueryHandler<GetFeatureFlagsQuery, IReadOnlyList<FeatureFlagAdminDto>>
{
    public async Task<Result<IReadOnlyList<FeatureFlagAdminDto>>> Handle(
        GetFeatureFlagsQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<FeatureFlagAdminDto> flags = await adminReadRepository.GetAllAsync(cancellationToken);
        return Result.Success(flags);
    }
}