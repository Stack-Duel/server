using FluentValidation;

namespace StackDuel.Application.Commands.Problems.RequiredLanguages.RemoveRequiredProblemLanguage;

internal sealed class RemoveRequiredProblemLanguageValidator : AbstractValidator<RemoveRequiredProblemLanguageCommand>
{
    public RemoveRequiredProblemLanguageValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}