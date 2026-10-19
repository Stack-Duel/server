using FluentValidation;

namespace StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

internal sealed class CalculatePlayerRankingsFromGameValidator : AbstractValidator<CalculatePlayerRankingsFromGameCommand>
{
    public CalculatePlayerRankingsFromGameValidator()
    {
        RuleFor(command => command.GameId).NotEmpty();

        RuleFor(command => command.SeasonId).NotEmpty();
    }
}