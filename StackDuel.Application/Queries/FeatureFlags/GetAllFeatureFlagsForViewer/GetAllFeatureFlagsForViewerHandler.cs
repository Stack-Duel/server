using StackDuel.Application.FeatureFlags;
using StackDuel.Domain.FeatureFlags;
using Ardalis.Result;

namespace StackDuel.Application.Queries.FeatureFlags.GetAllFeatureFlagsForViewer;

internal sealed class GetAllFeatureFlagsForViewerHandler(IFeatureFlagEvaluationRepository evaluationRepository)
    : IQueryHandler<GetAllFeatureFlagsForViewerQuery, IReadOnlyDictionary<string, bool>>
{
    public async Task<Result<IReadOnlyDictionary<string, bool>>> Handle(
        GetAllFeatureFlagsForViewerQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyDictionary<string, FeatureFlagEvaluationData> allData =
            await evaluationRepository.GetAllEvaluationDataAsync(request.UserId, cancellationToken);

        Dictionary<string, bool> resolved = allData.ToDictionary(
            kvp => kvp.Key,
            kvp => FeatureFlagEvaluator.Evaluate(kvp.Value, request.UserId)
        );

        return Result.Success<IReadOnlyDictionary<string, bool>>(resolved);
    }
}