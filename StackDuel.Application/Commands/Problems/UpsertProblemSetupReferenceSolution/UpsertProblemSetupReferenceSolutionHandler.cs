using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;

internal sealed class UpsertProblemSetupReferenceSolutionHandler(
    IValidator<UpsertProblemSetupReferenceSolutionCommand> validator,
    IProblemRepository problemRepository,
    IExecutionPipelineRepository executionPipelineRepository
) : AbstractCommandHandler<UpsertProblemSetupReferenceSolutionCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        UpsertProblemSetupReferenceSolutionCommand request,
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

        ProblemSetup? setup = problem.FindSetupByLanguageVersionId(request.LanguageVersionId);

        if (setup is null)
        {
            Guid? pipelineId = await executionPipelineRepository.FindIdByNameAsync(
                WellKnownExecutionPipelines.DefaultName,
                cancellationToken
            );

            if (pipelineId is null)
                return Result.Error("No execution pipeline is configured.");

            setup = problem.AddSetup(
                request.LanguageVersionId,
                request.InitialCode,
                request.FunctionName,
                pipelineId.Value
            );
        }
        else
        {
            problem.UpdateSetupInitialCode(setup.Id, request.InitialCode);
            problem.UpdateSetupFunctionName(setup.Id, request.FunctionName);
        }

        problem.SetReferenceSolution(setup.Id, request.ReferenceSolutionCode);

        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success(setup.Id);
    }
}