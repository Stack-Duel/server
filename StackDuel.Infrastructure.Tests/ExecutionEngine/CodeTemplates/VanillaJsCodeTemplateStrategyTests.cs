using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.IO.Compression;
using System.Text;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class VanillaJsCodeTemplateStrategyTests
{
    private readonly VanillaJsCodeTemplateStrategy _strategy = new();

    private static CodeTemplateContext Context(
        string? functionName = "solve",
        IReadOnlyList<CodeTemplateSourceFile>? additionalFiles = null
    ) => new("function solve() { return 1; }", functionName, [], additionalFiles);

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
    public void LanguageName_IsVanillaJs()
    {
        Assert.Equal("Vanilla JS", _strategy.LanguageName);
    }

    [Fact]
    public void Render_ProducesNothingBecauseTheBundleCarriesTheCode()
    {
        Assert.Equal(string.Empty, _strategy.Render(Context()));
    }

    [Fact]
    public void BuildAdditionalFiles_BundlesTheRuntimeAndTheWrappedSolution()
    {
        byte[]? bundle = _strategy.BuildAdditionalFiles(Context());

        Assert.NotNull(bundle);
        Dictionary<string, string> entries = ReadZip(bundle);
        Assert.Contains("harness.js", entries.Keys);
        Assert.Contains("solution.js", entries.Keys);
        Assert.Contains("compile", entries.Keys);
        Assert.Contains("run", entries.Keys);
        Assert.Contains("module.exports.Solution = solve;", entries["solution.js"]);
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
            Context(additionalFiles: [new CodeTemplateSourceFile("helper", "module.exports.x = 1;")])
        );

        Dictionary<string, string> entries = ReadZip(bundle!);
        Assert.Contains("helper.js", entries.Keys);
        Assert.Equal("module.exports.x = 1;", entries["helper.js"]);
    }

    [Theory]
    [InlineData("harness")]
    [InlineData("harness.js")]
    [InlineData("solution")]
    public void BuildAdditionalFiles_RefusesToLetASubmittedFileReplaceARuntimeEntry(string path)
    {
        string realHarness = Encoding.UTF8.GetString(VanillaJsRuntimeAssets.Harness);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () =>
                _strategy.BuildAdditionalFiles(
                    Context(additionalFiles: [new CodeTemplateSourceFile(path, "console.log('cheat');")])
                )
        );

        Assert.Contains("reserved", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cheat", realHarness);
    }
}