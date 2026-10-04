using StackDuel.Domain.FeatureFlags.ValueObjects;
using FluentValidation;

namespace StackDuel.Application.Commands.FeatureFlags.CreateFeatureFlag;

internal sealed class CreateFeatureFlagValidator : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(FeatureFlagKey.MaxLength)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Key must be lowercase kebab-case, e.g. 'leaderboards'.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotNull().MaximumLength(1000);
    }
}