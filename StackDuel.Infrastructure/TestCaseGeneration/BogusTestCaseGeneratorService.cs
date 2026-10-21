using Bogus;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using System.Text.Json;

namespace StackDuel.Infrastructure.TestCaseGeneration;

internal sealed class BogusTestCaseGeneratorService : ITestCaseGeneratorService
{
    private const string DefaultCharset = "abcdefghijklmnopqrstuvwxyz";
    private const int DefaultIntegerMin = -1000;
    private const int DefaultIntegerMax = 1000;
    private const double DefaultDoubleMin = 0;
    private const double DefaultDoubleMax = 1000;
    private const int DefaultLengthMin = 1;
    private const int DefaultLengthMax = 10;

    public IReadOnlyList<GeneratedValue> GenerateInputs(TestCaseGenerationSpec spec, int caseIndex)
    {
        var random = new Randomizer(unchecked((spec.Seed * 397) ^ caseIndex));

        return [.. spec.Parameters.Select(parameter => GenerateValue(parameter, random))];
    }

    private static GeneratedValue GenerateValue(GenerationParameterSpec parameter, Randomizer random)
    {
        ValueTypeSpec effective = EffectiveGenerationSpec.For(parameter);
        object? value = Generate(effective, random);

        return new GeneratedValue(Encode(effective, value), parameter.ValueType);
    }

    private static object? Generate(ValueTypeSpec spec, Randomizer random) =>
        spec.Kind switch
        {
            ValueTypeKind.Integer => random.Int(
                (int)(spec.Min ?? DefaultIntegerMin),
                (int)(spec.Max ?? DefaultIntegerMax)
            ),
            ValueTypeKind.Double => random.Double(spec.Min ?? DefaultDoubleMin, spec.Max ?? DefaultDoubleMax),
            ValueTypeKind.Boolean => random.Bool(),
            ValueTypeKind.String => GenerateString(spec, random),
            ValueTypeKind.Array => GenerateArray(spec, random),
            _ => throw new NotSupportedException($"Unsupported generation value type kind '{spec.Kind}'."),
        };

    private static string Encode(ValueTypeSpec spec, object? value) =>
        spec.Kind switch
        {
            ValueTypeKind.Integer => ((int)value!).ToString(),
            ValueTypeKind.Double => ((double)value!).ToString("R"),
            ValueTypeKind.Boolean => (bool)value! ? "true" : "false",
            _ => JsonSerializer.Serialize(value),
        };

    private static string GenerateString(ValueTypeSpec spec, Randomizer random)
    {
        (int minLength, int maxLength) = ResolveLength(spec);
        string charset = string.IsNullOrEmpty(spec.Charset) ? DefaultCharset : spec.Charset;

        return random.String2(minLength, maxLength, charset);
    }

    private static List<object?> GenerateArray(ValueTypeSpec spec, Randomizer random)
    {
        (int minLength, int maxLength) = ResolveLength(spec);
        int length = random.Int(minLength, maxLength);
        ValueTypeSpec element =
            spec.Element ?? throw new NotSupportedException("An array value type requires an element type.");

        List<object?> values = new(length);

        for (int index = 0; index < length; index++)
            values.Add(Generate(element, random));

        return values;
    }

    private static (int MinLength, int MaxLength) ResolveLength(ValueTypeSpec spec)
    {
        int minLength = spec.LengthMin ?? DefaultLengthMin;
        int maxLength = spec.LengthMax ?? Math.Max(minLength, DefaultLengthMax);

        return (minLength, maxLength);
    }
}