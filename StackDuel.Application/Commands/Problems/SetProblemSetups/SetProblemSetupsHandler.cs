using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Languages;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Application.Commands.Problems.SetProblemSetups;

internal sealed class SetProblemSetupsHandler(
    IValidator<SetProblemSetupsCommand> validator,
    IProblemRepository problemRepository,
    IExecutionPipelineRepository executionPipelineRepository,
    ILanguageVersionResolver languageVersionResolver
) : AbstractCommandHandler<SetProblemSetupsCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetProblemSetupsCommand request,
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

        Guid? pipelineId = await executionPipelineRepository.FindIdByNameAsync(
            WellKnownExecutionPipelines.DefaultName,
            cancellationToken
        );

        if (pipelineId is null)
            return Result.Error("No execution pipeline is configured.");

        List<(Guid LanguageVersionId, ProblemSetupInputDto Input)> resolved = [];

        foreach (ProblemSetupInputDto input in request.Setups)
        {
            Result<Guid> resolution = await languageVersionResolver.ResolveAsync(
                new LanguageVersionReference(input.LanguageVersionId, input.Language, input.LanguageVersion),
                cancellationToken
            );

            if (!resolution.IsSuccess)
                return Result.Invalid([.. resolution.ValidationErrors]);

            resolved.Add((resolution.Value, input));
        }

        if (resolved.Select(entry => entry.LanguageVersionId).Distinct().Count() != resolved.Count)
            return Result.Invalid(new ValidationError("Each language version may only appear once."));

        foreach ((Guid languageVersionId, ProblemSetupInputDto input) in resolved)
            ApplySetup(problem, languageVersionId, input, pipelineId.Value);

        await problemRepository.UpdateAsync(problem, cancellationToken);

        return Result.Success();
    }

    private static void ApplySetup(
        Problem problem,
        Guid languageVersionId,
        ProblemSetupInputDto input,
        Guid pipelineId
    )
    {
        ProblemSetup? setup = problem.FindSetupByLanguageVersionId(languageVersionId);

        if (setup is null)
        {
            setup = problem.AddSetup(languageVersionId, input.InitialCode, input.FunctionName, pipelineId);
        }
        else
        {
            problem.UpdateSetupInitialCode(setup.Id, input.InitialCode);
            problem.UpdateSetupFunctionName(setup.Id, input.FunctionName);
        }

        problem.SetReferenceSolution(setup.Id, input.ReferenceSolutionCode);
    }
}