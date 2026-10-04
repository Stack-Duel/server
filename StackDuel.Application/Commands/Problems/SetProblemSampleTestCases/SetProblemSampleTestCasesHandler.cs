using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.TestSuites;
using StackDuel.Domain.TestSuites.ValueObjects;
using Ardalis.Result;
using FluentValidation;

namespace StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;

internal sealed class SetProblemSampleTestCasesHandler(
    IValidator<SetProblemSampleTestCasesCommand> validator,
    IProblemRepository problemRepository,
    ITestSuiteWriteRepository testSuiteWriteRepository
) : AbstractCommandHandler<SetProblemSampleTestCasesCommand>(validator)
{
    protected override async Task<Result> HandleValidated(
        SetProblemSampleTestCasesCommand request,
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

        List<Guid> setupIds = [.. problem.Setups.Select(s => s.Id)];

        if (setupIds.Count == 0)
            return Result.Invalid(
                new ValidationError("TestCases", "Add at least one language setup before authoring sample test cases.")
            );

        List<AuthoredTestCaseSpec> specs =
        [
            .. request.TestCases.Select(tc => new AuthoredTestCaseSpec(
                tc.Name,
                [.. tc.Inputs.Select(i => (i.Value, i.ValueType))],
                tc.ExpectedOutputValue,
                tc.ExpectedOutputValueType
            )),
        ];

        string suiteName = $"{problem.Slug.Value} - Sample Cases";

        await testSuiteWriteRepository.ReplaceSampleTestCasesAsync(setupIds, suiteName, specs, cancellationToken);

        return Result.Success();
    }
}