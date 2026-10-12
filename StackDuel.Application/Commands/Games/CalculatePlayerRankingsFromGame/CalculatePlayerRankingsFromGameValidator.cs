
namespace StackDuel.Application.Commands.Games.CalculatePlayerRankingsFromGame;

internal sealed class CalculatPlayerRankingsFromGameValidator : AbstractValidator<CalculatePlayerRankingsFromGameCommand>
{
    public CalculatPlayerRankingsFromGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
    }
}