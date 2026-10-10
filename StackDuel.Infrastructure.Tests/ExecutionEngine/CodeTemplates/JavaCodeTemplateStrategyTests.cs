using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.IO.Compression;
using System.Text;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class JavaCodeTemplateStrategyTests
{
    private static readonly JavaCodeTemplateStrategy Strategy = new();

    private static CodeTemplateContext Context(
        string userCode = "class Solution { int solve(int a) { return a; } }",
        string? functionName = "solve",
        params (string Value, string ValueType)[] inputs
    ) => new(userCode, functionName, [.. inputs.Select(i => new CodeTemplateInput(i.Value, i.ValueType))]);

    private static Dictionary<string, string> ReadBundle(byte[] zipBytes)
    {
        using MemoryStream stream = new(zipBytes);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);

        Dictionary<string, string> entries = [];
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using StreamReader reader = new(entry.Open(), Encoding.UTF8);
            entries[entry.FullName] = reader.ReadToEnd();
        }

        return entries;
    }

    [Fact]
    public void LanguageName_IsJava()
    {
        Assert.Equal("Java", Strategy.LanguageName);
    }

    [Fact]
    public void Render_IsEmptyBecauseJavaShipsAFileBundleInstead()
    {
        Assert.Equal(string.Empty, Strategy.Render(Context()));
    }

    [Fact]
    public void BuildStdin_PutsOneArgumentPerLine()
    {
        string stdin = Strategy.BuildStdin([
            new CodeTemplateInput("[2,7,11]", "integer_array"),
            new CodeTemplateInput("9", "integer"),
        ]);

        Assert.Equal("[2,7,11]\n9", stdin);
    }

    [Fact]
    public void BuildStdin_NoInputs_IsEmpty()
    {
        Assert.Equal(string.Empty, Strategy.BuildStdin([]));
    }

    [Fact]
    public void BuildAdditionalFiles_BundlesSolutionMainAndTheCompileAndRunScripts()
    {
        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context())!);

        Assert.Equal(
            ["Solution.java", "Main.java", "compile", "run"],
            bundle.Keys.OrderBy(k => Array.IndexOf<string>(["Solution.java", "Main.java", "compile", "run"], k))
        );
    }

    [Fact]
    public void BuildAdditionalFiles_SolutionJavaHoldsTheSubmittedCode()
    {
        Dictionary<string, string> bundle = ReadBundle(
            Strategy.BuildAdditionalFiles(Context(userCode: "class Solution { /* MARKER */ }"))!
        );

        Assert.Contains("/* MARKER */", bundle["Solution.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_MainJavaCallsTheEntryPointOnASolutionInstance()
    {
        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context(functionName: "twoSum"))!);

        Assert.Contains("Solution solution = new Solution();", bundle["Main.java"]);
        Assert.Contains("solution.twoSum(", bundle["Main.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_CompileAndRunScriptsTargetTheJdkOnTheImage()
    {
        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context())!);

        Assert.Contains("javac", bundle["compile"]);
        Assert.Contains("java Main", bundle["run"]);
    }

    [Theory]
    [InlineData("integer", "int", "parseInt")]
    [InlineData("double", "double", "parseDouble")]
    [InlineData("boolean", "boolean", "parseBoolean")]
    [InlineData("string", "String", "parseString")]
    [InlineData("integer_array", "int[]", "parseIntArray")]
    public void BuildAdditionalFiles_DeclaresEachInputWithItsJavaTypeAndParser(
        string valueType,
        string javaType,
        string parseMethod
    )
    {
        Dictionary<string, string> bundle = ReadBundle(
            Strategy.BuildAdditionalFiles(Context(inputs: [("0", valueType)]))!
        );

        Assert.Contains($"{javaType} arg0 = {parseMethod}(__in.readLine());", bundle["Main.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_NumbersArgumentsInOrderAndPassesThemAll()
    {
        Dictionary<string, string> bundle = ReadBundle(
            Strategy.BuildAdditionalFiles(Context(inputs: [("[1]", "integer_array"), ("2", "integer")]))!
        );

        Assert.Contains("int[] arg0 = parseIntArray(__in.readLine());", bundle["Main.java"]);
        Assert.Contains("int arg1 = parseInt(__in.readLine());", bundle["Main.java"]);
        Assert.Contains("solution.solve(arg0, arg1)", bundle["Main.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_NoInputs_CallsTheEntryPointWithNoArguments()
    {
        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context())!);

        Assert.Contains("solution.solve()", bundle["Main.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_UnsupportedValueType_Throws()
    {
        Assert.Throws<NotSupportedException>(() =>
            Strategy.BuildAdditionalFiles(Context(inputs: [("{}", "linked_list")]))
        );
    }

    [Fact]
    public void BuildAdditionalFiles_AddsTheImportForAJavaUtilTypeTheCodeUsesButDidNotImport()
    {
        Dictionary<string, string> bundle = ReadBundle(
            Strategy.BuildAdditionalFiles(
                Context(userCode: "class Solution { List<Integer> solve() { return new ArrayList<>(); } }")
            )!
        );

        Assert.Contains("import java.util.List;", bundle["Solution.java"]);
        Assert.Contains("import java.util.ArrayList;", bundle["Solution.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_DoesNotDuplicateAnImportTheCodeAlreadyHas()
    {
        const string userCode = "import java.util.List;\nclass Solution { List<Integer> solve() { return null; } }";

        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context(userCode: userCode))!);

        Assert.Equal(1, bundle["Solution.java"].Split("import java.util.List;").Length - 1);
    }

    [Fact]
    public void BuildAdditionalFiles_WildcardImport_IsLeftAlone()
    {
        const string userCode = "import java.util.*;\nclass Solution { List<Integer> solve() { return null; } }";

        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context(userCode: userCode))!);

        Assert.Equal(userCode, bundle["Solution.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_CodeUsingNoJavaUtilTypes_IsLeftAlone()
    {
        const string userCode = "class Solution { int solve(int a) { return a + 1; } }";

        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context(userCode: userCode))!);

        Assert.Equal(userCode, bundle["Solution.java"]);
    }

    [Fact]
    public void BuildAdditionalFiles_PutsAddedImportsAboveTheSubmittedCode()
    {
        const string userCode = "class Solution { Map<String,Integer> solve() { return null; } }";

        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context(userCode: userCode))!);

        string solution = bundle["Solution.java"];
        Assert.True(
            solution.IndexOf("import java.util.Map;", StringComparison.Ordinal)
                < solution.IndexOf("class Solution", StringComparison.Ordinal),
            "imports must precede the class declaration to be valid Java"
        );
    }

    [Fact]
    public void BuildAdditionalFiles_OnlyMatchesWholeClassNames()
    {
        // "MyList" must not be mistaken for a use of java.util.List.
        const string userCode = "class Solution { MyListThing solve() { return null; } }";

        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context(userCode: userCode))!);

        Assert.DoesNotContain("import java.util.List;", bundle["Solution.java"]);
    }
}