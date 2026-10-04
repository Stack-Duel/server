using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Problems.AddProblemSetup;

internal sealed class AddProblemSetupHandler(
    IValidator<AddProblemSetupCommand> validator,
    IProblemRepository problemRepository,
    IExecutionPipelineRepository executionPipelineRepository
) : AbstractCommandHandler<AddProblemSetupCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        AddProblemSetupCommand request,
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

        if (problem.FindSetupByLanguageVersionId(request.LanguageVersionId) is not null)
            return Result.Invalid(
                new ValidationError("LanguageVersionId", "A setup for this language already exists.")
            );

        Guid? pipelineId = await executionPipelineRepository.FindIdByNameAsync(
            WellKnownExecutionPipelines.DefaultName,
            cancellationToken
        );

        if (pipelineId is null)
            return Result.Error("No execution pipeline is configured.");

        ProblemSetup setup = problem.AddSetup(
            request.LanguageVersionId,
            PlaceholderInitialCode,
            functionName: null,
            pipelineId.Value
        );

        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success(setup.Id);
    }

    private const string PlaceholderInitialCode = "// TODO: starter code";
}