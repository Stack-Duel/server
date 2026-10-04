using FluentValidation;

namespace StackDuel.Application.Commands.Games.CreateGame;

internal sealed class CreateGameValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameValidator()
    {
        RuleFor(x => x.GameModeKey).NotEmpty();
        RuleFor(x => x.TrackSelections).NotEmpty();
        RuleForEach(x => x.TrackSelections)
            .ChildRules(selection =>
            {
                selection.RuleFor(s => s.TrackKey).NotEmpty();
                selection.RuleFor(s => s.LanguageIds).NotEmpty();
            });
        RuleFor(x => x.TimeLimitInSeconds).GreaterThan(0);
        RuleFor(x => x.CreatedByUserId).NotEmpty();
    }
}