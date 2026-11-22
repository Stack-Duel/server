using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Commands.Problems.DeleteProblem;

internal sealed class DeleteProblemHandler(
    IValidator<DeleteProblemCommand> validator,
    IProblemRepository problemRepository
) : AbstractCommandHandler<DeleteProblemCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        DeleteProblemCommand request,
        CancellationToken cancellationToken
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(request.ProblemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        if (!string.Equals(request.ConfirmationTitle.Trim(), problem.Title.Value, StringComparison.Ordinal))
            return Result.Invalid(
                new ValidationError(
                    "ConfirmationTitle",
                    "The title you typed does not match this problem's title."
                )
            );

        if (problem.IsEditLocked)
            return Result.Invalid(
                new ValidationError("Status", "Wait for validation to finish before deleting this problem.")
            );

        problem.Delete();
        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }
}