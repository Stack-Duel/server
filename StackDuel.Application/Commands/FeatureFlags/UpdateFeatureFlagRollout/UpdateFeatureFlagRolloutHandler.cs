using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.FeatureFlags;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.FeatureFlags.ValueObjects;

namespace StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout;

internal sealed class UpdateFeatureFlagRolloutHandler(
    IValidator<UpdateFeatureFlagRolloutCommand> validator,
    IFeatureFlagWriteRepository writeRepository
) : AbstractCommandHandler<UpdateFeatureFlagRolloutCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        UpdateFeatureFlagRolloutCommand request,
        CancellationToken cancellationToken
    )
    {
        FeatureFlag? flag = await writeRepository.FindByIdAsync(request.FlagId, cancellationToken);

        if (flag is null)
            return Result.NotFound();

        flag.SetRolloutPercentage(new RolloutPercentage(request.RolloutPercentage));
        await writeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}