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

        RuleFor(x => x.Min)
            .Must(min => min is null)
            .When(x => GenerationValueTypes.IsSupported(x.ValueType) && !GenerationValueTypes.SupportsRange(x.ValueType))
            .WithMessage(parameter => $"A {parameter.ValueType} parameter has no minimum or maximum.");

        RuleFor(x => x.Max)
            .Must(max => max is null)
            .When(x => GenerationValueTypes.IsSupported(x.ValueType) && !GenerationValueTypes.SupportsRange(x.ValueType))
            .WithMessage(parameter => $"A {parameter.ValueType} parameter has no minimum or maximum.");

        RuleFor(x => x.LengthMin)
            .Must(lengthMin => lengthMin is null)
            .When(x =>
                GenerationValueTypes.IsSupported(x.ValueType) && !GenerationValueTypes.SupportsLength(x.ValueType)
            )
            .WithMessage(parameter => $"A {parameter.ValueType} parameter has no length.");

        RuleFor(x => x.LengthMax)
            .Must(lengthMax => lengthMax is null)
            .When(x =>
                GenerationValueTypes.IsSupported(x.ValueType) && !GenerationValueTypes.SupportsLength(x.ValueType)
            )
            .WithMessage(parameter => $"A {parameter.ValueType} parameter has no length.");

        RuleFor(x => x.Charset)
            .Must(charset => charset is null)
            .When(x =>
                GenerationValueTypes.IsSupported(x.ValueType) && !GenerationValueTypes.SupportsCharset(x.ValueType)
            )
            .WithMessage(parameter => $"A {parameter.ValueType} parameter has no charset.");
    }
}