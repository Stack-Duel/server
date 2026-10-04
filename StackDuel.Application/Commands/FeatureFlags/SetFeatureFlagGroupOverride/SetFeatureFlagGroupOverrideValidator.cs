using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagGroupOverride;

internal sealed class SetFeatureFlagGroupOverrideValidator : AbstractValidator<SetFeatureFlagGroupOverrideCommand>
{
    public SetFeatureFlagGroupOverrideValidator()
    {
        RuleFor(x => x.FlagId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Effect).IsInEnum();
    }
}