using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.ProblemValidation;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;

namespace StackDuel.Application.Commands.Problems.CancelProblemValidation;

internal sealed class CancelProblemValidationHandler(
    IValidator<CancelProblemValidationCommand> validator,
    IProblemRepository problemRepository,
    IProblemValidationJobRepository problemValidationJobRepository,
    IProblemValidationConcurrencyGuard concurrencyGuard
) : AbstractCommandHandler<CancelProblemValidationCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        CancelProblemValidationCommand request,
        CancellationToken cancellationToken
    ) =>
        await concurrencyGuard.RunExclusiveAsync(
            request.ProblemId,
            ct => CancelAsync(request.ProblemId, ct),
            cancellationToken
        );

    private async Task<Result> CancelAsync(Guid problemId, CancellationToken cancellationToken)
    {
        Problem? problem = await problemRepository.FindByIdAsync(problemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        if (!problem.IsEditLocked)
            return Result.Invalid(
                new ValidationError("Status", "Only a problem that is currently validating can be cancelled.")
            );

        ProblemValidationJob? job = await problemValidationJobRepository.FindActiveForProblemAsync(
            problemId,
            cancellationToken
        );

        if (job is not null)
        {
            job.Cancel();
            await problemValidationJobRepository.UpdateAsync(job, cancellationToken);
        }

        problem.CancelValidation();
        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }
}