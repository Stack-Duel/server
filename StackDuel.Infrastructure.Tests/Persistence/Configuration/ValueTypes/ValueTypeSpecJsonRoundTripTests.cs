using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using System.Text.Json;

namespace StackDuel.Infrastructure.Tests.Persistence.Configuration.ValueTypes;

/// <summary>
/// Exercises the exact serialize/deserialize delegates that
/// <c>ValueTypeDefinitionConfiguration</c> wires up via <c>HasConversion</c> for the
/// <c>spec</c> jsonb column. <see cref="ValueTypeSpec"/> has get-only properties and a single
/// public constructor whose parameter names match them, so these tests confirm
/// System.Text.Json can round-trip it through that constructor without any custom converter.
/// </summary>
public class ValueTypeSpecJsonRoundTripTests
{
    private static string Serialize(ValueTypeSpec spec) => JsonSerializer.Serialize(spec, (JsonSerializerOptions?)null);

    private static ValueTypeSpec Deserialize(string json) =>
        JsonSerializer.Deserialize<ValueTypeSpec>(json, (JsonSerializerOptions?)null)!;

    [Fact]
    public void RoundTrip_SimpleScalarSpec_PreservesKind()
    {
        var spec = new ValueTypeSpec(ValueTypeKind.Integer);

        ValueTypeSpec result = Deserialize(Serialize(spec));

        Assert.Equal(ValueTypeKind.Integer, result.Kind);
        Assert.Null(result.Min);
        Assert.Null(result.Max);
        Assert.Null(result.LengthMin);
        Assert.Null(result.LengthMax);
        Assert.Null(result.Charset);
        Assert.Null(result.Element);
    }

    [Fact]
    public void RoundTrip_ScalarSpecWithBounds_PreservesAllFields()
    {
        var spec = new ValueTypeSpec(ValueTypeKind.Integer, min: -5, max: 100);

        ValueTypeSpec result = Deserialize(Serialize(spec));

        Assert.Equal(ValueTypeKind.Integer, result.Kind);
        Assert.Equal(-5, result.Min);
        Assert.Equal(100, result.Max);
    }

    [Fact]
    public void RoundTrip_StringSpecWithLengthAndCharset_PreservesAllFields()
    {
        var spec = new ValueTypeSpec(ValueTypeKind.String, lengthMin: 1, lengthMax: 10, charset: "abcXYZ123");

        ValueTypeSpec result = Deserialize(Serialize(spec));

        Assert.Equal(ValueTypeKind.String, result.Kind);
        Assert.Equal(1, result.LengthMin);
        Assert.Equal(10, result.LengthMax);
        Assert.Equal("abcXYZ123", result.Charset);
    }

    [Fact]
    public void RoundTrip_NestedArrayOfIntegerWithConstraints_PreservesElementAndAllFields()
    {
        var element = new ValueTypeSpec(ValueTypeKind.Integer, min: 0, max: 1000);
        var spec = new ValueTypeSpec(ValueTypeKind.Array, lengthMin: 1, lengthMax: 50, element: element);

        ValueTypeSpec result = Deserialize(Serialize(spec));

        Assert.Equal(ValueTypeKind.Array, result.Kind);
        Assert.Equal(1, result.LengthMin);
        Assert.Equal(50, result.LengthMax);
        Assert.NotNull(result.Element);
        Assert.Equal(ValueTypeKind.Integer, result.Element!.Kind);
        Assert.Equal(0, result.Element.Min);
        Assert.Equal(1000, result.Element.Max);
        Assert.Equal(2, result.Depth);
    }

    [Fact]
    public void RoundTrip_DeeplyNestedMatrix_PreservesFullTree()
    {
        var innermost = new ValueTypeSpec(ValueTypeKind.Double, min: -1.5, max: 1.5);
        var middle = new ValueTypeSpec(ValueTypeKind.Array, element: innermost);
        var outer = new ValueTypeSpec(ValueTypeKind.Array, element: middle);

        ValueTypeSpec result = Deserialize(Serialize(outer));

        Assert.Equal(ValueTypeKind.Array, result.Kind);
        Assert.Equal(ValueTypeKind.Array, result.Element!.Kind);
        Assert.Equal(ValueTypeKind.Double, result.Element.Element!.Kind);
        Assert.Equal(-1.5, result.Element.Element.Min);
        Assert.Equal(1.5, result.Element.Element.Max);
        Assert.Equal(3, result.Depth);
    }
}