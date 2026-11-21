using FluentValidation;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;

internal sealed class GenerationParameterSpecValidator : AbstractValidator<GenerationParameterSpec>
{
    public GenerationParameterSpecValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.ValueType)
            .Must(GenerationValueTypes.IsSupported)
            .WithMessage(parameter =>
                $"'{parameter.ValueType}' is not a supported value type. Use one of: "
                + $"{string.Join(", ", GenerationValueTypes.Supported)}."
            );

        RuleFor(x => x.Max)
            .GreaterThanOrEqualTo(x => x.Min!.Value)
            .When(x => x.Min.HasValue && x.Max.HasValue)
            .WithMessage("Max must be greater than or equal to min.");

        RuleFor(x => x.LengthMin)
            .InclusiveBetween(0, GenerationValueTypes.MaxLength)
            .When(x => x.LengthMin.HasValue);

        RuleFor(x => x.LengthMax)
            .InclusiveBetween(0, GenerationValueTypes.MaxLength)
            .When(x => x.LengthMax.HasValue);

        RuleFor(x => x.LengthMax)
            .GreaterThanOrEqualTo(x => x.LengthMin!.Value)
            .When(x => x.LengthMin.HasValue && x.LengthMax.HasValue)
            .WithMessage("Maximum length must be greater than or equal to minimum length.");

        RuleFor(x => x.Charset).MaximumLength(GenerationValueTypes.MaxCharsetLength);

        RuleFor(x => x.Charset)
            .Must(charset => !string.IsNullOrEmpty(charset))
            .When(x => x.Charset is not null && GenerationValueTypes.SupportsCharset(x.ValueType))
            .WithMessage("Charset must not be blank when provided.");
    }
}