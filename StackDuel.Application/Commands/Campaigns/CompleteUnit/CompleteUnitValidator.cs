using FluentValidation;

namespace StackDuel.Application.Commands.Campaigns.CompleteUnit;

internal sealed class CompleteUnitValidator : AbstractValidator<CompleteUnitCommand>
{
    public CompleteUnitValidator()
    {
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}