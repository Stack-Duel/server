using FluentValidation;

namespace StackDuel.Application.Commands.Games.StartGame;

internal sealed class StartGameValidator : AbstractValidator<StartGameCommand>
{
    public StartGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}