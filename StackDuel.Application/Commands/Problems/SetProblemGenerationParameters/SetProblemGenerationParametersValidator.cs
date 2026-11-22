using FluentValidation;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;

internal sealed class SetProblemGenerationParametersValidator : AbstractValidator<SetProblemGenerationParametersCommand>
{
    public SetProblemGenerationParametersValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();

        RuleFor(x => x.Parameters).NotEmpty();

        RuleForEach(x => x.Parameters).SetValidator(new GenerationParameterSpecValidator());

        RuleFor(x => x.OutputValueType)
            .Must(GenerationValueTypes.IsSupported)
            .WithMessage(command =>
                $"'{command.OutputValueType}' is not a supported output value type. Use one of: "
                + $"{string.Join(", ", GenerationValueTypes.Supported)}."
            );

        RuleFor(x => x.TargetCaseCount).InclusiveBetween(1, GenerationValueTypes.MaxTargetCaseCount);
    }
}