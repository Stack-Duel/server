using FluentValidation;

namespace StackDuel.Application.Commands.Games.ForfeitGame;

internal sealed class ForfeitGameValidator : AbstractValidator<ForfeitGameCommand>
{
    public ForfeitGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}