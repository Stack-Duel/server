using FluentValidation;

namespace StackDuel.Application.Commands.Problems.AddProblemSetup;

internal sealed class AddProblemSetupValidator : AbstractValidator<AddProblemSetupCommand>
{
    public AddProblemSetupValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.LanguageVersionId).NotEmpty();
    }
}