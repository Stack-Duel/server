using FluentValidation.Results;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using SetProblemGenerationParametersCommand = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersCommand;
using SetProblemGenerationParametersValidator = StackDuel.Application.Commands.Problems.SetProblemGenerationParameters.SetProblemGenerationParametersValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SetProblemGenerationParameters;

public class SetProblemGenerationParametersValidatorTests
{
    private readonly SetProblemGenerationParametersValidator _validator = new();

    private static GenerationParameterSpec Parameter(
        string name = "numbers",
        string valueType = GenerationValueTypes.IntegerArray,
        double? min = -5,
        double? max = 5,
        int? lengthMin = 1,
        int? lengthMax = 20,
        string? charset = null
    ) => new(name, valueType, min, max, lengthMin, lengthMax, charset);

    private SetProblemGenerationParametersCommand Command(
        GenerationParameterSpec? parameter = null,
        string outputValueType = GenerationValueTypes.Integer,
        int targetCaseCount = 40
    ) => new(Guid.NewGuid(), [parameter ?? Parameter()], outputValueType, targetCaseCount, 7);

    private ValidationResult Validate(SetProblemGenerationParametersCommand command) =>
        _validator.Validate(command);

    [Fact]
    public void AValidSpecPasses()
    {
        Assert.True(Validate(Command()).IsValid);
    }

    [Fact]
    public void TargetCaseCountAboveTheCapIsRejected()
    {
        ValidationResult result = Validate(
            Command(targetCaseCount: GenerationValueTypes.MaxTargetCaseCount + 1)
        );

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "TargetCaseCount");
    }

    [Fact]
    public void TargetCaseCountAtTheCapIsAllowed()
    {
        Assert.True(Validate(Command(targetCaseCount: GenerationValueTypes.MaxTargetCaseCount)).IsValid);
    }

    [Fact]
    public void ZeroTargetCaseCountIsRejected()
    {
        Assert.False(Validate(Command(targetCaseCount: 0)).IsValid);
    }

    [Fact]
    public void AnUnsupportedParameterValueTypeIsRejected()
    {
        ValidationResult result = Validate(Command(Parameter(valueType: "integar")));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("not a supported value type"));
    }

    [Fact]
    public void AnUnsupportedOutputValueTypeIsRejected()
    {
        ValidationResult result = Validate(Command(outputValueType: "int"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "OutputValueType");
    }

    [Fact]
    public void MinGreaterThanMaxIsRejected()
    {
        ValidationResult result = Validate(Command(Parameter(min: 10, max: 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("greater than or equal to min"));
    }

    [Fact]
    public void LengthMinGreaterThanLengthMaxIsRejected()
    {
        ValidationResult result = Validate(Command(Parameter(lengthMin: 20, lengthMax: 2)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("minimum length"));
    }

    [Fact]
    public void LengthAboveTheCapIsRejected()
    {
        ValidationResult result = Validate(
            Command(Parameter(lengthMin: 1, lengthMax: GenerationValueTypes.MaxLength + 1))
        );

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains("LengthMax"));
    }

    [Fact]
    public void NegativeLengthIsRejected()
    {
        Assert.False(Validate(Command(Parameter(lengthMin: -1, lengthMax: 5))).IsValid);
    }

    [Fact]
    public void AnEmptyParameterNameIsRejectedByTheDomain()
    {
        Assert.Throws<ArgumentException>(() => Parameter(name: " "));
    }

    [Fact]
    public void NoParametersIsRejected()
    {
        SetProblemGenerationParametersCommand command = new(
            Guid.NewGuid(),
            [],
            GenerationValueTypes.Integer,
            40,
            7
        );

        Assert.False(Validate(command).IsValid);
    }

    [Fact]
    public void AnOversizedCharsetIsRejected()
    {
        ValidationResult result = Validate(
            Command(
                Parameter(
                    valueType: GenerationValueTypes.String,
                    min: null,
                    max: null,
                    charset: new string('a', GenerationValueTypes.MaxCharsetLength + 1)
                )
            )
        );

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ABlankCharsetOnAStringParameterIsRejected()
    {
        ValidationResult result = Validate(
            Command(Parameter(valueType: GenerationValueTypes.String, min: null, max: null, charset: ""))
        );

        Assert.False(result.IsValid);
    }
}