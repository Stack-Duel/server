using Ardalis.Result;
using StackDuel.Application.FeatureFlags;
using StackDuel.Application.FeatureFlags.Dtos;

namespace StackDuel.Application.Queries.FeatureFlags.GetFeatureFlagByKey;

internal sealed class GetFeatureFlagByKeyHandler(IFeatureFlagAdminReadRepository adminReadRepository)
    : IQueryHandler<GetFeatureFlagByKeyQuery, FeatureFlagAdminDto>
{
    public async Task<Result<FeatureFlagAdminDto>> Handle(
        GetFeatureFlagByKeyQuery request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlagAdminDto? flag = await adminReadRepository.GetByKeyAsync(request.Key, cancellationToken);

        if (flag is null)
            return Result.NotFound();

        return Result.Success(flag);
    }
}