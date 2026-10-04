using FluentValidation;

namespace StackDuel.Application.Commands.Games.JoinGame;

internal sealed class JoinGameValidator : AbstractValidator<JoinGameCommand>
{
    public JoinGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}