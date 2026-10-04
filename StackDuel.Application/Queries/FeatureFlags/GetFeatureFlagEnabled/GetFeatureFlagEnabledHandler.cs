using StackDuel.Application.FeatureFlags;
using StackDuel.Domain.FeatureFlags;
using Ardalis.Result;

namespace StackDuel.Application.Queries.FeatureFlags.GetFeatureFlagEnabled;

internal sealed class GetFeatureFlagEnabledHandler(IFeatureFlagEvaluationRepository evaluationRepository)
    : IQueryHandler<GetFeatureFlagEnabledQuery, bool>
{
    public async Task<Result<bool>> Handle(GetFeatureFlagEnabledQuery request, CancellationToken cancellationToken)
    {
        FeatureFlagEvaluationData? data = await evaluationRepository.GetEvaluationDataAsync(
            request.FlagKey,
            request.UserId,
            cancellationToken
        );

        if (data is null)
            return Result.NotFound();

        return Result.Success(FeatureFlagEvaluator.Evaluate(data, request.UserId));
    }
}