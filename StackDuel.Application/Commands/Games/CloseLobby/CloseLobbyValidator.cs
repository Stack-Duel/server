using FluentValidation;

namespace StackDuel.Application.Commands.Games.CloseLobby;

internal sealed class CloseLobbyValidator : AbstractValidator<CloseLobbyCommand>
{
    public CloseLobbyValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
    }
}