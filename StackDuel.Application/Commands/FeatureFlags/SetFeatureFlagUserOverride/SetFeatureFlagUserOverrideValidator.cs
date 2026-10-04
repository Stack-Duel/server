using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.SetFeatureFlagUserOverride;

internal sealed class SetFeatureFlagUserOverrideValidator : AbstractValidator<SetFeatureFlagUserOverrideCommand>
{
    public SetFeatureFlagUserOverrideValidator()
    {
        RuleFor(x => x.FlagId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Effect).IsInEnum();
    }
}