using Bogus;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using System.Text.Json;

namespace StackDuel.Infrastructure.TestCaseGeneration;

internal sealed class BogusTestCaseGeneratorService : ITestCaseGeneratorService
{
    private const string DefaultCharset = "abcdefghijklmnopqrstuvwxyz";

    public IReadOnlyList<GeneratedValue> GenerateInputs(TestCaseGenerationSpec spec, int caseIndex)
    {
        var random = new Randomizer(unchecked((spec.Seed * 397) ^ caseIndex));

        return [.. spec.Parameters.Select(parameter => GenerateValue(parameter, random))];
    }

    private static GeneratedValue GenerateValue(GenerationParameterSpec parameter, Randomizer random)
    {
        return parameter.ValueType switch
        {
            "integer" => new GeneratedValue(
                random.Int((int)(parameter.Min ?? -1000), (int)(parameter.Max ?? 1000)).ToString(),
                parameter.ValueType
            ),

            "double" => new GeneratedValue(
                random.Double(parameter.Min ?? 0, parameter.Max ?? 1000).ToString("R"),
                parameter.ValueType
            ),

            "boolean" => new GeneratedValue(random.Bool() ? "true" : "false", parameter.ValueType),

            "string" => new GeneratedValue(
                JsonSerializer.Serialize(GenerateString(parameter, random)),
                parameter.ValueType
            ),

            "integer_array" => new GeneratedValue(
                JsonSerializer.Serialize(GenerateIntArray(parameter, random)),
                parameter.ValueType
            ),

            _ => throw new NotSupportedException($"Unsupported generation value type '{parameter.ValueType}'."),
        };
    }

    private static string GenerateString(GenerationParameterSpec parameter, Randomizer random)
    {
        int minLength = parameter.LengthMin ?? 1;
        int maxLength = parameter.LengthMax ?? Math.Max(minLength, 10);
        string charset = string.IsNullOrEmpty(parameter.Charset) ? DefaultCharset : parameter.Charset;

        return random.String2(minLength, maxLength, charset);
    }

    private static int[] GenerateIntArray(GenerationParameterSpec parameter, Randomizer random)
    {
        int minLength = parameter.LengthMin ?? 1;
        int maxLength = parameter.LengthMax ?? Math.Max(minLength, 10);
        int length = random.Int(minLength, maxLength);

        int min = (int)(parameter.Min ?? -1000);
        int max = (int)(parameter.Max ?? 1000);

        int[] values = new int[length];
        for (int i = 0; i < length; i++)
            values[i] = random.Int(min, max);

        return values;
    }
}