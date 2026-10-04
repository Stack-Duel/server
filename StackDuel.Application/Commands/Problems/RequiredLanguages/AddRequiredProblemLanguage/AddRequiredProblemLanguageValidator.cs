using FluentValidation;

namespace StackDuel.Application.Commands.Problems.RequiredLanguages.AddRequiredProblemLanguage;

internal sealed class AddRequiredProblemLanguageValidator : AbstractValidator<AddRequiredProblemLanguageCommand>
{
    public AddRequiredProblemLanguageValidator()
    {
        RuleFor(x => x.LanguageVersionId).NotEmpty();
    }
}