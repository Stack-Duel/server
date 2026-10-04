using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.UpdateFeatureFlagDefault;

internal sealed class UpdateFeatureFlagDefaultValidator : AbstractValidator<UpdateFeatureFlagDefaultCommand>
{
    public UpdateFeatureFlagDefaultValidator()
    {
        RuleFor(x => x.FlagId).NotEmpty();
    }
}