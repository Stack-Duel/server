using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.TestSuites.Enums;
using Ardalis.Result;
using FluentValidation;

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

        if (problem.Status is not (ProblemStatus.Draft or ProblemStatus.Failed))
            return Result.Invalid(
                new ValidationError("Status", $"Cannot submit a problem for validation from status {problem.Status}.")
            );

        List<string> errors = [];

        if (problem.Setups.Count == 0)
            errors.Add("At least one language setup is required.");

        foreach (ProblemSetup setup in problem.Setups.Where(s => string.IsNullOrWhiteSpace(s.ReferenceSolutionCode)))
            errors.Add($"A reference solution is required for setup {setup.Id}.");

        bool hasSampleTestCases = problem
            .Setups.SelectMany(s => s.TestSuites)
            .Where(ts => ts.Type == TestSuiteType.Sample)
            .SelectMany(ts => ts.TestCases)
            .Any(tc => tc.Source == TestCaseSource.Authored && tc.RetiredAt is null);

        if (!hasSampleTestCases)
            errors.Add("At least one sample test case is required.");

        if (problem.GenerationSpec is null)
            errors.Add("Generation parameters are required before submitting for validation.");

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
}