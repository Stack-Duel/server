using StackDuel.Domain.ValueTypes.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates.ValueTypes;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates.ValueTypes;

public class CppValueTypeRendererTests
{
    private static ValueTypeSpec Scalar(ValueTypeKind kind) => new(kind);

    private static ValueTypeSpec ArrayOf(ValueTypeSpec element) => new(ValueTypeKind.Array, element: element);

    [Fact]
    public void RenderType_Integer_IsInt() => Assert.Equal("int", CppValueTypeRenderer.RenderType(Scalar(ValueTypeKind.Integer)));

    [Fact]
    public void RenderType_Double_IsDouble() =>
        Assert.Equal("double", CppValueTypeRenderer.RenderType(Scalar(ValueTypeKind.Double)));

    [Fact]
    public void RenderType_Boolean_IsBool() =>
        Assert.Equal("bool", CppValueTypeRenderer.RenderType(Scalar(ValueTypeKind.Boolean)));

    [Fact]
    public void RenderType_String_IsString() =>
        Assert.Equal("string", CppValueTypeRenderer.RenderType(Scalar(ValueTypeKind.String)));

    [Fact]
    public void RenderType_IntegerArray_IsVectorInt() =>
        Assert.Equal("vector<int>", CppValueTypeRenderer.RenderType(ArrayOf(Scalar(ValueTypeKind.Integer))));

    [Fact]
    public void RenderType_NestedIntegerArray_IsVectorOfVectorInt() =>
        Assert.Equal(
            "vector<vector<int>>",
            CppValueTypeRenderer.RenderType(ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer))))
        );

    [Fact]
    public void RenderType_StringArray_IsVectorString() =>
        Assert.Equal("vector<string>", CppValueTypeRenderer.RenderType(ArrayOf(Scalar(ValueTypeKind.String))));

    [Fact]
    public void ParserName_IntegerArray_KeepsTheLongStandingName() =>
        Assert.Equal("parseIntArray", CppValueTypeRenderer.ParserName(ArrayOf(Scalar(ValueTypeKind.Integer))));

    [Fact]
    public void ParserName_NestedArray_ExtendsTheNamingRule() =>
        Assert.Equal(
            "parseIntArrayArray",
            CppValueTypeRenderer.ParserName(ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer))))
        );

    [Fact]
    public void ParserName_StringArray_KeepsTheLongStandingName() =>
        Assert.Equal("parseStringArray", CppValueTypeRenderer.ParserName(ArrayOf(Scalar(ValueTypeKind.String))));

    [Fact]
    public void RenderArrayParsers_ScalarOnly_EmitsNothing() =>
        Assert.Equal(
            string.Empty,
            CppValueTypeRenderer.RenderArrayParsers([Scalar(ValueTypeKind.Integer), Scalar(ValueTypeKind.String)])
        );

    [Fact]
    public void RenderArrayParsers_IntegerArray_EmitsSplitterAndParser()
    {
        string rendered = CppValueTypeRenderer.RenderArrayParsers([ArrayOf(Scalar(ValueTypeKind.Integer))]);

        Assert.Contains("static vector<string> __splitTopLevel(const string& raw)", rendered);
        Assert.Contains("static vector<int> parseIntArray(const string& raw)", rendered);
        Assert.Contains("result.push_back(parseInt(part));", rendered);
    }

    [Fact]
    public void RenderArrayParsers_NestedArray_EmitsBothLevels()
    {
        string rendered = CppValueTypeRenderer.RenderArrayParsers([ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer)))]);

        Assert.Contains("static vector<int> parseIntArray(const string& raw)", rendered);
        Assert.Contains("static vector<vector<int>> parseIntArrayArray(const string& raw)", rendered);
        Assert.Contains("result.push_back(parseIntArray(part));", rendered);
    }

    [Fact]
    public void RenderArrayParsers_NestedArray_DeclaresInnermostParserFirst()
    {
        string rendered = CppValueTypeRenderer.RenderArrayParsers([ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer)))]);

        int innerIndex = rendered.IndexOf("static vector<int> parseIntArray(", StringComparison.Ordinal);
        int outerIndex = rendered.IndexOf("static vector<vector<int>> parseIntArrayArray(", StringComparison.Ordinal);

        Assert.True(innerIndex >= 0 && outerIndex >= 0);
        Assert.True(innerIndex < outerIndex, "parseIntArray must be declared before parseIntArrayArray uses it");
    }

    [Fact]
    public void RenderArrayParsers_StringArray_UsesStringParser()
    {
        string rendered = CppValueTypeRenderer.RenderArrayParsers([ArrayOf(Scalar(ValueTypeKind.String))]);

        Assert.Contains("static vector<string> parseStringArray(const string& raw)", rendered);
        Assert.Contains("result.push_back(parseString(part));", rendered);
    }

    [Fact]
    public void RenderArrayParsers_RepeatedAndOverlappingSpecs_EmitEachParserOnce()
    {
        string rendered = CppValueTypeRenderer.RenderArrayParsers(
            [
                ArrayOf(Scalar(ValueTypeKind.Integer)),
                ArrayOf(ArrayOf(Scalar(ValueTypeKind.Integer))),
                ArrayOf(Scalar(ValueTypeKind.Integer)),
            ]
        );

        Assert.Equal(1, CountOccurrences(rendered, "static vector<int> parseIntArray(const string& raw)"));
        Assert.Equal(
            1,
            CountOccurrences(rendered, "static vector<vector<int>> parseIntArrayArray(const string& raw)")
        );
        Assert.Equal(1, CountOccurrences(rendered, "static vector<string> __splitTopLevel(const string& raw)"));
    }

    [Fact]
    public void RenderArrayParsers_DeclaresEveryParserItCalls()
    {
        string rendered = CppValueTypeRenderer.RenderArrayParsers(
            [ArrayOf(ArrayOf(Scalar(ValueTypeKind.String))), ArrayOf(Scalar(ValueTypeKind.Double))]
        );

        Assert.Contains("static vector<string> parseStringArray(const string& raw)", rendered);
        Assert.Contains("static vector<vector<string>> parseStringArrayArray(const string& raw)", rendered);
        Assert.Contains("static vector<double> parseDoubleArray(const string& raw)", rendered);
    }

    [Fact]
    public void SupportedKinds_CoversEveryKindTheSpecModelAllows() =>
        Assert.Equal(Enum.GetValues<ValueTypeKind>().ToHashSet(), CppValueTypeRenderer.SupportedKinds.ToHashSet());

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