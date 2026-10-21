using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.TestCaseGeneration;
using System.Text.Json;

namespace StackDuel.Infrastructure.Tests.TestCaseGeneration;

public class BogusTestCaseGeneratorServiceTests
{
    private readonly BogusTestCaseGeneratorService _generator = new();

    private static GenerationParameterSpec Parameter(
        string valueType,
        double? min = null,
        double? max = null,
        int? lengthMin = null,
        int? lengthMax = null,
        string? charset = null,
        ValueTypeSpec? resolvedType = null
    ) => new("p", valueType, min, max, lengthMin, lengthMax, charset, resolvedType);

    private static ValueTypeSpec ArrayOf(ValueTypeSpec element) => new(ValueTypeKind.Array, element: element);

    private static TestCaseGenerationSpec Spec(params GenerationParameterSpec[] parameters) =>
        new(Guid.NewGuid(), parameters, WellKnownValueTypes.Integer, targetCaseCount: 5, seed: 1234, version: 1);

    private string GenerateOne(GenerationParameterSpec parameter, int caseIndex = 0) =>
        _generator.GenerateInputs(Spec(parameter), caseIndex).Single().Value;

    [Fact]
    public void Integer_IsEncodedAsBareText()
    {
        string value = GenerateOne(Parameter(WellKnownValueTypes.Integer, min: 5, max: 5));

        Assert.Equal("5", value);
    }

    [Fact]
    public void Integer_RespectsItsRange()
    {
        for (int caseIndex = 0; caseIndex < 25; caseIndex++)
        {
            int value = int.Parse(GenerateOne(Parameter(WellKnownValueTypes.Integer, min: -3, max: 3), caseIndex));

            Assert.InRange(value, -3, 3);
        }
    }

    [Fact]
    public void Boolean_IsEncodedAsBareTrueOrFalse()
    {
        string value = GenerateOne(Parameter(WellKnownValueTypes.Boolean));

        Assert.Contains(value, new[] { "true", "false" });
    }

    [Fact]
    public void Double_IsEncodedAsBareTextWithinRange()
    {
        string value = GenerateOne(Parameter(WellKnownValueTypes.Double, min: 1, max: 2));

        Assert.DoesNotContain("\"", value);
        Assert.InRange(double.Parse(value), 1, 2);
    }

    [Fact]
    public void String_IsEncodedAsJsonAndHonoursCharsetAndLength()
    {
        string value = GenerateOne(
            Parameter(WellKnownValueTypes.String, lengthMin: 3, lengthMax: 3, charset: "ab")
        );

        string decoded = JsonSerializer.Deserialize<string>(value)!;
        Assert.Equal(3, decoded.Length);
        Assert.All(decoded, character => Assert.Contains(character, "ab"));
    }

    [Fact]
    public void IntegerArray_IsEncodedAsJsonWithLengthAndElementBounds()
    {
        string value = GenerateOne(
            Parameter(WellKnownValueTypes.IntegerArray, min: 0, max: 4, lengthMin: 2, lengthMax: 2)
        );

        int[] decoded = JsonSerializer.Deserialize<int[]>(value)!;
        Assert.Equal(2, decoded.Length);
        Assert.All(decoded, element => Assert.InRange(element, 0, 4));
    }

    [Fact]
    public void Matrix_GeneratesNestedArraysWithoutAnyDedicatedValueType()
    {
        ValueTypeSpec matrix = ArrayOf(ArrayOf(new ValueTypeSpec(ValueTypeKind.Integer)));

        string value = GenerateOne(
            Parameter("int_matrix", min: 1, max: 1, lengthMin: 2, lengthMax: 2, resolvedType: matrix)
        );

        int[][] decoded = JsonSerializer.Deserialize<int[][]>(value)!;
        Assert.Equal(2, decoded.Length);
        Assert.All(decoded, row => Assert.All(row, element => Assert.Equal(1, element)));
    }

    [Fact]
    public void StringArray_GeneratesJsonStringsFromTheGivenCharset()
    {
        ValueTypeSpec stringArray = ArrayOf(new ValueTypeSpec(ValueTypeKind.String));

        string value = GenerateOne(
            Parameter("string_array", lengthMin: 2, lengthMax: 2, charset: "z", resolvedType: stringArray)
        );

        string[] decoded = JsonSerializer.Deserialize<string[]>(value)!;
        Assert.Equal(2, decoded.Length);
        Assert.All(decoded, element => Assert.All(element, character => Assert.Equal('z', character)));
    }

    [Fact]
    public void BooleanArray_GeneratesJsonBooleans()
    {
        ValueTypeSpec booleanArray = ArrayOf(new ValueTypeSpec(ValueTypeKind.Boolean));

        string value = GenerateOne(
            Parameter("bool_array", lengthMin: 3, lengthMax: 3, resolvedType: booleanArray)
        );

        bool[] decoded = JsonSerializer.Deserialize<bool[]>(value)!;
        Assert.Equal(3, decoded.Length);
    }

    [Fact]
    public void SameSpecAndCaseIndex_AlwaysProduceTheSameValue()
    {
        GenerationParameterSpec parameter = Parameter(WellKnownValueTypes.IntegerArray, min: -50, max: 50);
        TestCaseGenerationSpec spec = Spec(parameter);

        IReadOnlyList<GeneratedValue> first = _generator.GenerateInputs(spec, 7);
        IReadOnlyList<GeneratedValue> second = _generator.GenerateInputs(spec, 7);

        Assert.Equal(first.Single().Value, second.Single().Value);
    }

    [Fact]
    public void DifferentCaseIndexes_ProduceDifferentValues()
    {
        GenerationParameterSpec parameter = Parameter(WellKnownValueTypes.IntegerArray, min: -1000, max: 1000);
        TestCaseGenerationSpec spec = Spec(parameter);

        HashSet<string> values =
        [
            .. Enumerable.Range(0, 10).Select(caseIndex => _generator.GenerateInputs(spec, caseIndex).Single().Value),
        ];

        Assert.True(values.Count > 1);
    }

    [Fact]
    public void GeneratedValue_KeepsTheParametersValueTypeKey()
    {
        GeneratedValue generated = _generator
            .GenerateInputs(Spec(Parameter(WellKnownValueTypes.IntegerArray)), 0)
            .Single();

        Assert.Equal(WellKnownValueTypes.IntegerArray, generated.ValueType);
    }

    [Fact]
    public void MultipleParameters_AreGeneratedInOrder()
    {
        IReadOnlyList<GeneratedValue> generated = _generator.GenerateInputs(
            Spec(
                Parameter(WellKnownValueTypes.Integer, min: 1, max: 1),
                Parameter(WellKnownValueTypes.Boolean),
                Parameter(WellKnownValueTypes.Integer, min: 2, max: 2)
            ),
            0
        );

        Assert.Equal(3, generated.Count);
        Assert.Equal("1", generated[0].Value);
        Assert.Equal("2", generated[2].Value);
    }

    [Fact]
    public void UnknownValueTypeWithoutSnapshot_Throws()
    {
        Assert.Throws<NotSupportedException>(() => GenerateOne(Parameter("not_a_type")));
    }

    [Fact]
    public void Snapshot_IsUsedEvenWhenTheKeyNamesSomethingElse()
    {
        string value = GenerateOne(
            Parameter(
                WellKnownValueTypes.Integer,
                lengthMin: 2,
                lengthMax: 2,
                min: 8,
                max: 8,
                resolvedType: ArrayOf(new ValueTypeSpec(ValueTypeKind.Integer))
            )
        );

        Assert.Equal([8, 8], JsonSerializer.Deserialize<int[]>(value)!);
    }
}