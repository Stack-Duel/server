using FluentValidation;

namespace StackDuel.Application.Commands.Games.JoinGameByCode;

internal sealed class JoinGameByCodeValidator : AbstractValidator<JoinGameByCodeCommand>
{
    public JoinGameByCodeValidator()
    {
        RuleFor(x => x.JoinCode).NotEmpty().Length(7);
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}