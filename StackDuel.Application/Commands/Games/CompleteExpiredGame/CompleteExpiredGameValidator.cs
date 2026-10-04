using FluentValidation;

namespace StackDuel.Application.Commands.Games.CompleteExpiredGame;

internal sealed class CompleteExpiredGameValidator : AbstractValidator<CompleteExpiredGameCommand>
{
    public CompleteExpiredGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty().WithMessage("Game ID is required.");

        RuleFor(x => x.ExpectedStartedAt).NotEqual(default(DateTime)).WithMessage("ExpectedStartedAt is required.");

        RuleFor(x => x.RescheduleCount).GreaterThanOrEqualTo(0).WithMessage("RescheduleCount cannot be negative.");
    }
}