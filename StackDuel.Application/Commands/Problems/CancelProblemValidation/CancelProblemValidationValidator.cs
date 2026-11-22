using FluentValidation;

namespace StackDuel.Application.Commands.Problems.CancelProblemValidation;

internal sealed class CancelProblemValidationValidator : AbstractValidator<CancelProblemValidationCommand>
{
    public CancelProblemValidationValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
    }
}