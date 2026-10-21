using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates.ValueTypes;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates.ValueTypes;

public class JavaValueTypeRendererTests
{
    private static ValueTypeSpec Scalar(ValueTypeKind kind) => new(kind);

    private static ValueTypeSpec ArrayOf(ValueTypeSpec element) => new(ValueTypeKind.Array, element: element);

    [Fact]
    public void RenderType_Integer_IsInt() => Assert.Equal("int", JavaValueTypeRenderer.RenderType(Scalar(ValueTypeKind.Integer)));

    [Fact]
    public void RenderType_Double_IsDouble() =>
        Assert.Equal("double", JavaValueTypeRenderer.RenderType(Scalar(ValueTypeKind.Double)));

    [Fact]
    public void RenderType_Boolean_IsBoolean() =>
        Assert.Equal("boolean", JavaValueTypeRenderer.RenderType(Scalar(ValueTypeKind.Boolean)));

    [Fact]
    public void RenderType_String_IsString() =>
        Assert.Equal("String", JavaValueTypeRenderer.RenderType(Scalar(ValueTypeKind.String)));

    [Fact]
    public void RenderType_IntegerArray_IsIntArray() =>
        Assert.Equal("int[]", JavaValueTypeRenderer.RenderType(ArrayOf(Scalar(ValueTypeKind.Integer))));

    [Fact]
    public void RenderType_NestedIntegerArray_IsIntMatrix() =>
        Assert.Equal("int[][]", JavaValueTypeRenderer.RenderType(ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer)))));

    [Fact]
    public void RenderType_StringArray_IsStringArray() =>
        Assert.Equal("String[]", JavaValueTypeRenderer.RenderType(ArrayOf(Scalar(ValueTypeKind.String))));

    [Fact]
    public void ParserName_IntegerArray_KeepsTheLongStandingName() =>
        Assert.Equal("parseIntArray", JavaValueTypeRenderer.ParserName(ArrayOf(Scalar(ValueTypeKind.Integer))));

    [Fact]
    public void ParserName_NestedArray_ExtendsTheNamingRule() =>
        Assert.Equal(
            "parseIntArrayArray",
            JavaValueTypeRenderer.ParserName(ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer))))
        );

    [Fact]
    public void RenderArrayParsers_ScalarOnly_EmitsNothing() =>
        Assert.Equal(
            string.Empty,
            JavaValueTypeRenderer.RenderArrayParsers([Scalar(ValueTypeKind.Integer), Scalar(ValueTypeKind.String)])
        );

    [Fact]
    public void RenderArrayParsers_IntegerArray_EmitsSplitterAndParser()
    {
        string rendered = JavaValueTypeRenderer.RenderArrayParsers([ArrayOf(Scalar(ValueTypeKind.Integer))]);

        Assert.Contains("static String[] __splitTopLevel(String raw)", rendered);
        Assert.Contains("static int[] parseIntArray(String raw)", rendered);
        Assert.Contains("int[] result = new int[parts.length];", rendered);
        Assert.Contains("result[i] = parseInt(parts[i]);", rendered);
    }

    [Fact]
    public void RenderArrayParsers_NestedArray_EmitsBothLevelsWithJavaArrayCreation()
    {
        string rendered = JavaValueTypeRenderer.RenderArrayParsers([ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer)))]);

        Assert.Contains("static int[] parseIntArray(String raw)", rendered);
        Assert.Contains("static int[][] parseIntArrayArray(String raw)", rendered);
        Assert.Contains("int[][] result = new int[parts.length][];", rendered);
        Assert.Contains("result[i] = parseIntArray(parts[i]);", rendered);
    }

    [Fact]
    public void RenderArrayParsers_StringArray_UsesReferenceArrayCreation()
    {
        string rendered = JavaValueTypeRenderer.RenderArrayParsers([ArrayOf(Scalar(ValueTypeKind.String))]);

        Assert.Contains("static String[] parseStringArray(String raw)", rendered);
        Assert.Contains("String[] result = new String[parts.length];", rendered);
        Assert.Contains("result[i] = parseString(parts[i]);", rendered);
    }

    [Fact]
    public void RenderArrayParsers_RepeatedAndOverlappingSpecs_EmitEachParserOnce()
    {
        string rendered = JavaValueTypeRenderer.RenderArrayParsers(
            [
                ArrayOf(Scalar(ValueTypeKind.Integer)),
                ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer))),
                ArrayOf(Scalar(ValueTypeKind.Integer)),
            ]
        );

        Assert.Equal(1, CountOccurrences(rendered, "static int[] parseIntArray(String raw)"));
        Assert.Equal(1, CountOccurrences(rendered, "static int[][] parseIntArrayArray(String raw)"));
        Assert.Equal(1, CountOccurrences(rendered, "static String[] __splitTopLevel(String raw)"));
    }

    [Fact]
    public void RenderArrayParsers_DeclaresEveryParserItCalls()
    {
        string rendered = JavaValueTypeRenderer.RenderArrayParsers(
            [ArrayOf(ArrayOf(Scalar(ValueTypeKind.String))), ArrayOf(Scalar(ValueTypeKind.Double))]
        );

        Assert.Contains("static String[] parseStringArray(String raw)", rendered);
        Assert.Contains("static String[][] parseStringArrayArray(String raw)", rendered);
        Assert.Contains("static double[] parseDoubleArray(String raw)", rendered);
        Assert.Contains("String[][] result = new String[parts.length][];", rendered);
    }

    [Fact]
    public void SupportedKinds_CoversEveryKindTheSpecModelAllows() =>
        Assert.Equal(Enum.GetValues<ValueTypeKind>().ToHashSet(), JavaValueTypeRenderer.SupportedKinds.ToHashSet());

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }
}