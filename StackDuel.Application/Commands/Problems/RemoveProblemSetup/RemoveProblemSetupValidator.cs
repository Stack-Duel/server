using FluentValidation;

namespace StackDuel.Application.Commands.Problems.RemoveProblemSetup;

internal sealed class RemoveProblemSetupValidator : AbstractValidator<RemoveProblemSetupCommand>
{
    public RemoveProblemSetupValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.LanguageVersionId).NotEmpty();
    }
}