using FluentValidation;

namespace StackDuel.Application.Commands.Problems.SetProblemSetups;

internal sealed class SetProblemSetupsValidator : AbstractValidator<SetProblemSetupsCommand>
{
    public SetProblemSetupsValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.Setups).NotEmpty();

        RuleForEach(x => x.Setups)
            .ChildRules(setup =>
            {
                setup
                    .RuleFor(x => x)
                    .Must(NamesALanguage)
                    .WithMessage("Each setup needs either a languageVersionId or a language name.");
                setup.RuleFor(x => x.Language).MaximumLength(100).When(x => x.Language is not null);
                setup.RuleFor(x => x.LanguageVersion).MaximumLength(50).When(x => x.LanguageVersion is not null);
                setup.RuleFor(x => x.InitialCode).NotEmpty().MaximumLength(20_000);
                setup.RuleFor(x => x.FunctionName).MaximumLength(200).When(x => x.FunctionName is not null);
                setup.RuleFor(x => x.ReferenceSolutionCode).NotEmpty().MaximumLength(20_000);
            });
    }

    private static bool NamesALanguage(ProblemSetupInputDto setup) =>
        (setup.LanguageVersionId is { } languageVersionId && languageVersionId != Guid.Empty)
        || !string.IsNullOrWhiteSpace(setup.Language);
}