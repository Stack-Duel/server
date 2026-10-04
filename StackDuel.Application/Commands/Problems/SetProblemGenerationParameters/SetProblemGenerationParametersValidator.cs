using FluentValidation;

namespace StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;

internal sealed class SetProblemGenerationParametersValidator : AbstractValidator<SetProblemGenerationParametersCommand>
{
    public SetProblemGenerationParametersValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
        RuleFor(x => x.Parameters).NotEmpty();
        RuleFor(x => x.OutputValueType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetCaseCount).GreaterThan(0);
    }
}