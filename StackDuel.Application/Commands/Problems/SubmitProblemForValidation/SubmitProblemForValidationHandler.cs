using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;

namespace StackDuel.Application.Commands.Problems.SubmitProblemForValidation;

internal sealed class SubmitProblemForValidationHandler(
    IValidator<SubmitProblemForValidationCommand> validator,
    IProblemRepository problemRepository,
    IProblemValidationJobRepository problemValidationJobRepository,
    IMessagePublisher messagePublisher
) : AbstractCommandHandler<SubmitProblemForValidationCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SubmitProblemForValidationCommand request,
        CancellationToken cancellationToken
    )
    {
        Problem? problem = await problemRepository.FindByIdAsync(request.ProblemId, cancellationToken);

        if (problem is null)
            return Result.NotFound();

        if (!problem.CanSubmitForValidation)
            return Result.Invalid(
                new ValidationError("Status", $"Cannot submit a problem for validation from status {problem.Status}.")
            );

        ProblemPublishReadiness readiness = problem.EvaluatePublishReadiness();

        List<string> errors = [];

        if (!readiness.HasSetups)
            errors.Add("At least one language setup is required.");

        foreach (ProblemSetup setup in problem.SetupsMissingReferenceSolution())
            errors.Add($"A reference solution is required for setup {setup.Id}.");

        if (!readiness.HasSampleTestCases)
            errors.Add("At least one sample test case is required.");

        if (!readiness.HasGenerationParameters)
            errors.Add("Generation parameters are required before submitting for validation.");

        errors.AddRange(DescribeArityProblems(problem));

        if (errors.Count > 0)
        {
            List<ValidationError> validationErrors = [.. errors.Select(e => new ValidationError(e))];
            return Result.Invalid(validationErrors);
        }

        problem.SubmitForValidation();
        await problemRepository.UpdateAsync(problem, cancellationToken);

        ProblemValidationJob job = new(problem.Id);
        await problemValidationJobRepository.AddAsync(job, cancellationToken);

        await messagePublisher.PublishAsync(new ProblemValidationJobContinuationMessage(job.Id), cancellationToken);

        return Result.Success();
    }

    private static IEnumerable<string> DescribeArityProblems(Problem problem)
    {
        IReadOnlyList<int> inputCounts = problem.SampleTestCaseInputCounts();

        if (inputCounts.Count > 1)
        {
            yield return "Every sample test case must have the same number of inputs, but they have "
                + $"{string.Join(" and ", inputCounts)}.";
            yield break;
        }

        if (inputCounts.Count == 0 || problem.GenerationSpec is not { } generationSpec)
            yield break;

        int parameterCount = generationSpec.Parameters.Count;
        int inputCount = inputCounts[0];

        if (parameterCount != inputCount)
            yield return $"There are {parameterCount} generation parameter(s) but each sample test case has "
                + $"{inputCount} input(s). They have to match, because the generated inputs are passed to the "
                + "reference solution in the same order.";
    }
}