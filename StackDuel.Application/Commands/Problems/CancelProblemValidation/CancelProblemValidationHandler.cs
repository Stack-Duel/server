using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;

namespace StackDuel.Application.Commands.Problems.CancelProblemValidation;

internal sealed class CancelProblemValidationHandler(
    IValidator<CancelProblemValidationCommand> validator,
    IProblemRepository problemRepository,
    IProblemValidationJobRepository problemValidationJobRepository
) : AbstractCommandHandler<CancelProblemValidationCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        CancelProblemValidationCommand request,
        CancellationToken cancellationToken
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(request.ProblemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        if (problem.Status != ProblemStatus.Pending)
            return Result.Invalid(
                new ValidationError("Status", "Only a problem waiting on validation can be cancelled.")
            );

        ProblemValidationJob? job = await problemValidationJobRepository.FindUnfinishedByProblemIdAsync(
            request.ProblemId,
            cancellationToken
        );

        if (job is not null)
        {
            job.Cancel("Cancelled by an administrator.");
            await problemValidationJobRepository.UpdateAsync(job, cancellationToken);
        }

        problem.CancelValidation();
        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }
}