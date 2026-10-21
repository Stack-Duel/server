using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.ValueTypes;
using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;

namespace StackDuel.Domain.Tests.TestCaseGeneration.ValueObjects;

public class EffectiveGenerationSpecTests
{
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

    [Fact]
    public void For_BuiltInIntegerWithoutSnapshot_FallsBackToTheCatalog()
    {
        ValueTypeSpec effective = EffectiveGenerationSpec.For(Parameter(WellKnownValueTypes.Integer));

        Assert.Equal(ValueTypeKind.Integer, effective.Kind);
    }

    [Fact]
    public void For_UnknownValueTypeWithoutSnapshot_Throws()
    {
        NotSupportedException error = Assert.Throws<NotSupportedException>(() =>
            EffectiveGenerationSpec.For(Parameter("not_a_type"))
        );

        Assert.Contains("not_a_type", error.Message);
    }

    [Fact]
    public void For_IntegerOverrides_LandOnTheNumber()
    {
        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter(WellKnownValueTypes.Integer, min: 5, max: 9)
        );

        Assert.Equal(5, effective.Min);
        Assert.Equal(9, effective.Max);
    }

    [Fact]
    public void For_StringOverrides_LandOnTheString()
    {
        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter(WellKnownValueTypes.String, lengthMin: 2, lengthMax: 4, charset: "ab")
        );

        Assert.Equal(ValueTypeKind.String, effective.Kind);
        Assert.Equal(2, effective.LengthMin);
        Assert.Equal(4, effective.LengthMax);
        Assert.Equal("ab", effective.Charset);
    }

    [Fact]
    public void For_IntegerArray_BoundsTheArrayByLengthAndItsElementsByMinMax()
    {
        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter(WellKnownValueTypes.IntegerArray, min: -3, max: 3, lengthMin: 2, lengthMax: 5)
        );

        Assert.Equal(ValueTypeKind.Array, effective.Kind);
        Assert.Equal(2, effective.LengthMin);
        Assert.Equal(5, effective.LengthMax);
        Assert.Null(effective.Min);
        Assert.NotNull(effective.Element);
        Assert.Equal(ValueTypeKind.Integer, effective.Element.Kind);
        Assert.Equal(-3, effective.Element.Min);
        Assert.Equal(3, effective.Element.Max);
    }

    [Fact]
    public void For_Matrix_BoundsOnlyTheOuterArrayByLength()
    {
        ValueTypeSpec matrix = ArrayOf(ArrayOf(new ValueTypeSpec(ValueTypeKind.Integer)));

        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter("int_matrix", min: 0, max: 9, lengthMin: 2, lengthMax: 3, resolvedType: matrix)
        );

        Assert.Equal(2, effective.LengthMin);
        Assert.Equal(3, effective.LengthMax);
        Assert.Null(effective.Element!.LengthMin);
        Assert.Null(effective.Element.LengthMax);
        Assert.Equal(0, effective.Element.Element!.Min);
        Assert.Equal(9, effective.Element.Element.Max);
    }

    [Fact]
    public void For_ArrayOfStrings_PutsCharsetOnTheElementAndLengthOnTheArray()
    {
        ValueTypeSpec stringArray = ArrayOf(new ValueTypeSpec(ValueTypeKind.String));

        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter("string_array", lengthMin: 4, lengthMax: 6, charset: "xy", resolvedType: stringArray)
        );

        Assert.Equal(4, effective.LengthMin);
        Assert.Equal(6, effective.LengthMax);
        Assert.Equal("xy", effective.Element!.Charset);
        Assert.Null(effective.Element.LengthMin);
    }

    [Fact]
    public void For_Snapshot_WinsOverTheKey()
    {
        ValueTypeSpec snapshot = ArrayOf(new ValueTypeSpec(ValueTypeKind.Integer));

        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter(WellKnownValueTypes.Integer, resolvedType: snapshot)
        );

        Assert.Equal(ValueTypeKind.Array, effective.Kind);
    }

    [Fact]
    public void For_SnapshotConstraints_SurviveWhenTheParameterOverridesNothing()
    {
        ValueTypeSpec snapshot = new(ValueTypeKind.Integer, min: 10, max: 20);

        ValueTypeSpec effective = EffectiveGenerationSpec.For(Parameter("bounded_int", resolvedType: snapshot));

        Assert.Equal(10, effective.Min);
        Assert.Equal(20, effective.Max);
    }

    [Fact]
    public void For_ParameterOverrides_BeatSnapshotConstraints()
    {
        ValueTypeSpec snapshot = new(ValueTypeKind.Integer, min: 10, max: 20);

        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter("bounded_int", min: 1, max: 2, resolvedType: snapshot)
        );

        Assert.Equal(1, effective.Min);
        Assert.Equal(2, effective.Max);
    }

    [Fact]
    public void For_Boolean_IgnoresEveryOverride()
    {
        ValueTypeSpec effective = EffectiveGenerationSpec.For(
            Parameter(WellKnownValueTypes.Boolean, min: 1, max: 2, lengthMin: 3, lengthMax: 4, charset: "ab")
        );

        Assert.Equal(ValueTypeKind.Boolean, effective.Kind);
        Assert.Null(effective.Min);
        Assert.Null(effective.LengthMin);
        Assert.Null(effective.Charset);
    }
}