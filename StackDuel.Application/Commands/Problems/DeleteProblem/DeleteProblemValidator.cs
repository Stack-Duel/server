using FluentValidation;

namespace StackDuel.Application.Commands.Problems.DeleteProblem;

internal sealed class DeleteProblemValidator : AbstractValidator<DeleteProblemCommand>
{
    public DeleteProblemValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.ConfirmationTitle).NotEmpty();
    }
}