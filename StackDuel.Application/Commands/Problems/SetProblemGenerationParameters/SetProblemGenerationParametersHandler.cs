using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;

internal sealed class SetProblemGenerationParametersHandler(
    IValidator<SetProblemGenerationParametersCommand> validator,
    IProblemRepository problemRepository
) : AbstractCommandHandler<SetProblemGenerationParametersCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetProblemGenerationParametersCommand request,
        CancellationToken cancellationToken
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(request.ProblemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        if (problem.Status == ProblemStatus.Pending)
            return Result.Invalid(
                new ValidationError("Status", "Cannot edit a problem while validation is in progress.")
            );

        problem.SetGenerationParameters(
            request.Parameters,
            request.OutputValueType,
            request.TargetCaseCount,
            request.Seed
        );

        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }
}