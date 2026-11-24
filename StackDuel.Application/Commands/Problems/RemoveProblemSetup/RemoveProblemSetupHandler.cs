using Ardalis.Result;
using FluentValidation;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Commands.Problems.RemoveProblemSetup;

internal sealed class RemoveProblemSetupHandler(
    IValidator<RemoveProblemSetupCommand> validator,
    IProblemRepository problemRepository
) : AbstractCommandHandler<RemoveProblemSetupCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        RemoveProblemSetupCommand request,
        CancellationToken cancellationToken
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(request.ProblemId, cancellationToken);

        if (problem is null || problem.IsDeleted)
            return Result.NotFound();

        if (problem.IsEditLocked)
            return Result.Invalid(
                new ValidationError("Status", "Cannot edit a problem while validation is in progress.")
            );

        ProblemSetup? setup = problem.FindSetupByLanguageVersionId(request.LanguageVersionId);

        if (setup is null)
            return Result.NotFound();

        problem.RetireSetup(setup.Id);

        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }
}