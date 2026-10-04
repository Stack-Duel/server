using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagGroupOverride;

internal sealed class RemoveFeatureFlagGroupOverrideValidator : AbstractValidator<RemoveFeatureFlagGroupOverrideCommand>
{
    public RemoveFeatureFlagGroupOverrideValidator()
    {
        RuleFor(x => x.FlagId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
    }
}