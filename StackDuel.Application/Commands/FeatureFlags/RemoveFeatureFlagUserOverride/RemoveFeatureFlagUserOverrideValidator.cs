using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.RemoveFeatureFlagUserOverride;

internal sealed class RemoveFeatureFlagUserOverrideValidator : AbstractValidator<RemoveFeatureFlagUserOverrideCommand>
{
    public RemoveFeatureFlagUserOverrideValidator()
    {
        RuleFor(x => x.FlagId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}