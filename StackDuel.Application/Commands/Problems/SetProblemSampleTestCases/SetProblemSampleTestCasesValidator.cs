using FluentValidation;

namespace StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;

internal sealed class SetProblemSampleTestCasesValidator : AbstractValidator<SetProblemSampleTestCasesCommand>
{
    public SetProblemSampleTestCasesValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.TestCases).NotEmpty();

        RuleForEach(x => x.TestCases)
            .ChildRules(testCase =>
            {
                testCase.RuleFor(x => x.Inputs).NotEmpty();
                testCase
                    .RuleForEach(x => x.Inputs)
                    .ChildRules(input =>
                    {
                        input.RuleFor(x => x.Value).NotEmpty();
                        input.RuleFor(x => x.ValueType).NotEmpty();
                    });
                testCase.RuleFor(x => x.ExpectedOutputValue).NotEmpty();
                testCase.RuleFor(x => x.ExpectedOutputValueType).NotEmpty();
            });
    }
}