using FluentValidation;

namespace StackDuel.Application.Commands.Problems.SubmitProblemForValidation;

internal sealed class SubmitProblemForValidationValidator : AbstractValidator<SubmitProblemForValidationCommand>
{
    public SubmitProblemForValidationValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
    }
}