using Moq;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class CppCodeTemplateStrategyTests
{
    private static readonly CppCodeTemplateStrategy Strategy = new();

    private static CodeTemplateContext Context(
        string userCode = "struct Solution { int solve(int a) { return a; } };",
        string? functionName = "solve",
        params (string Value, string ValueType)[] inputs
    ) => new(userCode, functionName, [.. inputs.Select(i => new CodeTemplateInput(i.Value, i.ValueType))]);

    [Fact]
    public void LanguageName_IsCpp()
    {
        Assert.Equal("C++", Strategy.LanguageName);
    }

    [Fact]
    public void BuildStdin_PutsOneArgumentPerLine()
    {
        string stdin = Strategy.BuildStdin([
            new CodeTemplateInput("[2,7]", "integer_array"),
            new CodeTemplateInput("9", "integer"),
        ]);

        Assert.Equal("[2,7]\n9", stdin);
    }

    [Fact]
    public void Render_EmbedsTheSubmittedCodeAndCallsTheEntryPoint()
    {
        string rendered = Strategy.Render(Context(userCode: "// MARKER", functionName: "twoSum"));

        Assert.Contains("// MARKER", rendered);
        Assert.Contains("solution.twoSum(", rendered);
    }

    [Fact]
    public void Render_ConstructsASolutionInstanceAndPrintsTheResult()
    {
        string rendered = Strategy.Render(Context());

        Assert.Contains("Solution solution;", rendered);
        Assert.Contains("cout << toOutputString(result);", rendered);
    }

    [Theory]
    [InlineData("integer", "int", "parseInt")]
    [InlineData("double", "double", "parseDouble")]
    [InlineData("boolean", "bool", "parseBool")]
    [InlineData("string", "string", "parseString")]
    [InlineData("integer_array", "vector<int>", "parseIntArray")]
    public void Render_DeclaresEachInputWithItsCppTypeAndParser(string valueType, string cppType, string parseFn)
    {
        string rendered = Strategy.Render(Context(inputs: [("0", valueType)]));

        Assert.Contains($"{cppType} arg0 = {parseFn}(__readLine());", rendered);
    }

    [Fact]
    public void Render_NumbersArgumentsInOrderAndPassesThemAll()
    {
        string rendered = Strategy.Render(Context(inputs: [("[1]", "integer_array"), ("2", "integer")]));

        Assert.Contains("vector<int> arg0 = parseIntArray(__readLine());", rendered);
        Assert.Contains("int arg1 = parseInt(__readLine());", rendered);
        Assert.Contains("solution.solve(arg0, arg1)", rendered);
    }

    [Fact]
    public void Render_NoInputs_CallsTheEntryPointWithNoArguments()
    {
        Assert.Contains("solution.solve()", Strategy.Render(Context()));
    }

    [Fact]
    public void Render_UnsupportedValueType_Throws()
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            Strategy.Render(Context(inputs: [("{}", "linked_list")]))
        );

        Assert.Contains("linked_list", exception.Message);
    }

    [Fact]
    public void BuildAdditionalFiles_IsNullBecauseCppCompilesFromASingleFile()
    {
        Assert.Null(((ICodeTemplateStrategy)Strategy).BuildAdditionalFiles(Context()));
    }
}

public class VanillaJsCodeTemplateStrategyTests
{
    private static readonly VanillaJsCodeTemplateStrategy Strategy = new();

    private static CodeTemplateContext Context(
        string? functionName = "Counter",
        IReadOnlyList<CodeTemplateSourceFile>? additionalFiles = null
    ) => new("function Counter() {}", functionName, [], additionalFiles);

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
    public void LanguageName_IsVanillaJs()
    {
        Assert.Equal("Vanilla JS", Strategy.LanguageName);
    }

    [Fact]
    public void Render_IsEmptyBecauseVanillaJsShipsAFileBundleInstead()
    {
        Assert.Equal(string.Empty, Strategy.Render(Context()));
    }

    [Fact]
    public void BuildStdin_WrapsPropsAndActionsIntoOneObject()
    {
        string stdin = Strategy.BuildStdin([
            new CodeTemplateInput("""{"start":2}""", "props"),
            new CodeTemplateInput("""[{"type":"click"}]""", "actions"),
        ]);

        using JsonDocument document = JsonDocument.Parse(stdin);
        Assert.Equal(2, document.RootElement.GetProperty("props").GetProperty("start").GetInt32());
        Assert.Equal("click", document.RootElement.GetProperty("actions")[0].GetProperty("type").GetString());
    }

    [Fact]
    public void BuildStdin_MissingInputs_DefaultsToEmptyPropsAndActions()
    {
        string stdin = Strategy.BuildStdin([]);

        using JsonDocument document = JsonDocument.Parse(stdin);
        Assert.Empty(document.RootElement.GetProperty("props").EnumerateObject());
        Assert.Equal(0, document.RootElement.GetProperty("actions").GetArrayLength());
    }

    [Fact]
    public void BuildStdin_IgnoresInputsThatAreNeitherPropsNorActions()
    {
        string stdin = Strategy.BuildStdin([new CodeTemplateInput("""{"a":1}""", "something-else")]);

        using JsonDocument document = JsonDocument.Parse(stdin);
        Assert.Empty(document.RootElement.GetProperty("props").EnumerateObject());
    }

    [Fact]
    public void BuildAdditionalFiles_BundlesTheHarnessSolutionAndScripts()
    {
        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context())!);

        Assert.Contains("harness.js", bundle.Keys);
        Assert.Contains("solution.js", bundle.Keys);
        Assert.Contains("compile", bundle.Keys);
        Assert.Contains("run", bundle.Keys);
    }

    [Fact]
    public void BuildAdditionalFiles_ExportsTheNamedFunctionAsSolution()
    {
        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context("Counter"))!);

        Assert.Contains("module.exports.Solution = Counter;", bundle["solution.js"]);
    }

    [Fact]
    public void BuildAdditionalFiles_RunScriptStartsTheHarnessUnderNode()
    {
        Dictionary<string, string> bundle = ReadBundle(Strategy.BuildAdditionalFiles(Context())!);

        Assert.Contains("node harness.js", bundle["run"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildAdditionalFiles_WithoutAFunctionName_Throws(string? functionName)
    {
        Assert.Throws<InvalidOperationException>(() => Strategy.BuildAdditionalFiles(Context(functionName)));
    }

    [Fact]
    public void BuildAdditionalFiles_IncludesExtraFilesRenamedToDotJs()
    {
        Dictionary<string, string> bundle = ReadBundle(
            Strategy.BuildAdditionalFiles(
                Context(additionalFiles: [new CodeTemplateSourceFile("Helper.jsx", "// HELPER")])
            )!
        );

        Assert.Contains("Helper.js", bundle.Keys);
        Assert.Contains("// HELPER", bundle["Helper.js"]);
    }

    [Fact]
    public void BuildAdditionalFiles_ExtraFileAlreadyNamedDotJs_KeepsItsName()
    {
        Dictionary<string, string> bundle = ReadBundle(
            Strategy.BuildAdditionalFiles(
                Context(additionalFiles: [new CodeTemplateSourceFile("Helper.js", "// HELPER")])
            )!
        );

        Assert.Contains("Helper.js", bundle.Keys);
    }
}

public class ReactCodeTemplateStrategyTests
{
    private static Dictionary<string, byte[]> ReadBundle(byte[] zipBytes)
    {
        using MemoryStream stream = new(zipBytes);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);

        Dictionary<string, byte[]> entries = [];
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using MemoryStream entryStream = new();
            using (Stream source = entry.Open())
                source.CopyTo(entryStream);
            entries[entry.FullName] = entryStream.ToArray();
        }

        return entries;
    }

    private static Mock<IJsxTranspiler> PassThroughTranspiler()
    {
        Mock<IJsxTranspiler> transpiler = new();
        transpiler.Setup(t => t.Transpile(It.IsAny<string>())).Returns<string>(source => source);
        return transpiler;
    }

    private static CodeTemplateContext Context(
        string? functionName = "Counter",
        IReadOnlyList<CodeTemplateSourceFile>? additionalFiles = null
    ) => new("function Counter() { return <div />; }", functionName, [], additionalFiles);

    [Fact]
    public void LanguageName_IsReact()
    {
        Assert.Equal("React", new ReactCodeTemplateStrategy(PassThroughTranspiler().Object).LanguageName);
    }

    [Fact]
    public void Render_IsEmptyBecauseReactShipsAFileBundleInstead()
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        Assert.Equal(string.Empty, strategy.Render(Context()));
    }

    [Fact]
    public void BuildStdin_WrapsPropsAndActionsIntoOneObject()
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        string stdin = strategy.BuildStdin([
            new CodeTemplateInput("""{"start":5}""", "props"),
            new CodeTemplateInput("""[{"type":"click"}]""", "actions"),
        ]);

        using JsonDocument document = JsonDocument.Parse(stdin);
        Assert.Equal(5, document.RootElement.GetProperty("props").GetProperty("start").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("actions").GetArrayLength());
    }

    [Fact]
    public void BuildStdin_MissingInputs_DefaultsToEmptyPropsAndActions()
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        using JsonDocument document = JsonDocument.Parse(strategy.BuildStdin([]));

        Assert.Empty(document.RootElement.GetProperty("props").EnumerateObject());
        Assert.Equal(0, document.RootElement.GetProperty("actions").GetArrayLength());
    }

    [Fact]
    public void BuildAdditionalFiles_TranspilesTheSubmittedSourceBeforeBundlingIt()
    {
        Mock<IJsxTranspiler> transpiler = new();
        transpiler.Setup(t => t.Transpile(It.IsAny<string>())).Returns("// TRANSPILED");
        ReactCodeTemplateStrategy strategy = new(transpiler.Object);

        Dictionary<string, byte[]> bundle = ReadBundle(strategy.BuildAdditionalFiles(Context())!);

        Assert.Equal("// TRANSPILED", Encoding.UTF8.GetString(bundle["solution.js"]));
        transpiler.Verify(
            t => t.Transpile(It.Is<string>(s => s.Contains("module.exports.Solution = Counter;"))),
            Times.Once
        );
    }

    [Fact]
    public void BuildAdditionalFiles_RequiresReactAndExportsTheNamedComponent()
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        Dictionary<string, byte[]> bundle = ReadBundle(strategy.BuildAdditionalFiles(Context("Counter"))!);

        string solution = Encoding.UTF8.GetString(bundle["solution.js"]);
        Assert.Contains("""const React = require("react");""", solution);
        Assert.Contains("module.exports.Solution = Counter;", solution);
    }

    [Fact]
    public void BuildAdditionalFiles_IncludesTheReactRuntimeAndTheRunScripts()
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        Dictionary<string, byte[]> bundle = ReadBundle(strategy.BuildAdditionalFiles(Context())!);

        Assert.Contains("harness.js", bundle.Keys);
        Assert.Contains("compile", bundle.Keys);
        Assert.Contains("node harness.js", Encoding.UTF8.GetString(bundle["run"]));
        Assert.Contains(bundle.Keys, k => k.StartsWith("node_modules/react/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildAdditionalFiles_WithoutAFunctionName_Throws(string? functionName)
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        Assert.Throws<InvalidOperationException>(() => strategy.BuildAdditionalFiles(Context(functionName)));
    }

    [Fact]
    public void BuildAdditionalFiles_TranspilesExtraFilesTooAndRenamesThemToDotJs()
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        Dictionary<string, byte[]> bundle = ReadBundle(
            strategy.BuildAdditionalFiles(
                Context(additionalFiles: [new CodeTemplateSourceFile("Helper.jsx", "// HELPER")])
            )!
        );

        string helper = Encoding.UTF8.GetString(bundle["Helper.js"]);
        Assert.Contains("// HELPER", helper);
        Assert.Contains("""const React = require("react");""", helper);
    }

    [Fact]
    public void BuildAdditionalFiles_ExtraFileGetsNoSolutionExportBecauseItHasNoEntryPoint()
    {
        ReactCodeTemplateStrategy strategy = new(PassThroughTranspiler().Object);

        Dictionary<string, byte[]> bundle = ReadBundle(
            strategy.BuildAdditionalFiles(
                Context(additionalFiles: [new CodeTemplateSourceFile("Helper.jsx", "// HELPER")])
            )!
        );

        Assert.DoesNotContain("module.exports.Solution", Encoding.UTF8.GetString(bundle["Helper.js"]));
    }
}