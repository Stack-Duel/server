using Moq;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.IO.Compression;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class ReactCodeTemplateStrategyTests
{
    private readonly Mock<IJsxTranspiler> _transpiler = new();
    private readonly ReactCodeTemplateStrategy _strategy;

    public ReactCodeTemplateStrategyTests()
    {
        _transpiler.Setup(x => x.Transpile(It.IsAny<string>())).Returns<string>(source => source);
        _strategy = new ReactCodeTemplateStrategy(_transpiler.Object);
    }

    private static CodeTemplateContext Context(
        string? functionName = "Widget",
        IReadOnlyList<CodeTemplateSourceFile>? additionalFiles = null
    ) => new("const Widget = () => null;", functionName, [], additionalFiles);

    private static Dictionary<string, string> ReadZip(byte[] bundle)
    {
        using MemoryStream stream = new(bundle);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);

        Dictionary<string, string> entries = [];
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using StreamReader reader = new(entry.Open());
            entries[entry.FullName] = reader.ReadToEnd();
        }
        return entries;
    }

    [Fact]
    public void LanguageName_IsReact()
    {
        Assert.Equal("React", _strategy.LanguageName);
    }

    [Fact]
    public void Render_ProducesNothingBecauseTheBundleCarriesTheCode()
    {
        Assert.Equal(string.Empty, _strategy.Render(Context()));
    }

    [Fact]
    public void BuildStdin_CombinesThePropsAndActionsInputs()
    {
        string stdin = _strategy.BuildStdin(
            [new CodeTemplateInput("{\"a\":1}", "props"), new CodeTemplateInput("[\"click\"]", "actions")]
        );

        Assert.Contains("\"props\":{\"a\":1}", stdin);
        Assert.Contains("\"actions\":[\"click\"]", stdin);
    }

    [Fact]
    public void BuildStdin_DefaultsMissingInputsToEmptyShapes()
    {
        string stdin = _strategy.BuildStdin([]);

        Assert.Contains("\"props\":{}", stdin);
        Assert.Contains("\"actions\":[]", stdin);
    }

    [Fact]
    public void BuildAdditionalFiles_BundlesTheRuntimeAndTheWrappedSolution()
    {
        byte[]? bundle = _strategy.BuildAdditionalFiles(Context());

        Assert.NotNull(bundle);
        Dictionary<string, string> entries = ReadZip(bundle);
        Assert.Contains("solution.js", entries.Keys);
        Assert.Contains("compile", entries.Keys);
        Assert.Contains("run", entries.Keys);
        Assert.Contains("module.exports.Solution = Widget;", entries["solution.js"]);
    }

    [Fact]
    public void BuildAdditionalFiles_ThrowsWhenTheProblemHasNoFunctionName()
    {
        Assert.Throws<InvalidOperationException>(() => _strategy.BuildAdditionalFiles(Context(functionName: null)));
    }

    [Fact]
    public void BuildAdditionalFiles_IncludesASubmittedHelperFile()
    {
        byte[]? bundle = _strategy.BuildAdditionalFiles(
            Context(additionalFiles: [new CodeTemplateSourceFile("Helper", "module.exports.Helper = 1;")])
        );

        Dictionary<string, string> entries = ReadZip(bundle!);
        Assert.Contains("Helper.js", entries.Keys);
        Assert.Contains("module.exports.Helper = 1;", entries["Helper.js"]);
    }

    [Theory]
    [InlineData("harness")]
    [InlineData("solution")]
    public void BuildAdditionalFiles_RefusesToLetASubmittedFileReplaceARuntimeEntry(string path)
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () =>
                _strategy.BuildAdditionalFiles(
                    Context(additionalFiles: [new CodeTemplateSourceFile(path, "console.log('cheat');")])
                )
        );

        Assert.Contains("reserved", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}