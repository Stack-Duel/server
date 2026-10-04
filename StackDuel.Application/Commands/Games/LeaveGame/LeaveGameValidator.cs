using FluentValidation;

namespace StackDuel.Application.Commands.Games.LeaveGame;

internal sealed class LeaveGameValidator : AbstractValidator<LeaveGameCommand>
{
    public LeaveGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}