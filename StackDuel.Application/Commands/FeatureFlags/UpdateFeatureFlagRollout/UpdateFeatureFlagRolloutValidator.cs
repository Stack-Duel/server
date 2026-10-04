using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagRollout;

internal sealed class UpdateFeatureFlagRolloutValidator : AbstractValidator<UpdateFeatureFlagRolloutCommand>
{
    public UpdateFeatureFlagRolloutValidator()
    {
        RuleFor(x => x.FlagId).NotEmpty();
        RuleFor(x => x.RolloutPercentage).InclusiveBetween(0, 100);
    }
}