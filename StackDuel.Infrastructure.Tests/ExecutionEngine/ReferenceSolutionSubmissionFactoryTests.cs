using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.IO.Compression;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine;

public class ReferenceSolutionSubmissionFactoryTests
{
    private const int JavaMultiFileLanguageId = 89;
    private const int PythonLanguageId = 109;

    private static List<CodeTemplateInput> Inputs(params (string Value, string ValueType)[] inputs) =>
        [.. inputs.Select(i => new CodeTemplateInput(i.Value, i.ValueType))];

    [Fact]
    public void MultiFileLanguage_ShipsTheFileBundleRatherThanSourceCode()
    {
        ExecutionEngineSubmission submission = ReferenceSolutionSubmissionFactory.Create(
            new JavaCodeTemplateStrategy(),
            "class Solution { int minCoins(int amount) { return 0; } }",
            "minCoins",
            Inputs(("30", "integer")),
            JavaMultiFileLanguageId
        );

        Assert.Empty(submission.SourceCode);
        Assert.NotNull(submission.AdditionalFiles);
        Assert.NotEmpty(submission.AdditionalFiles!);
    }

    [Fact]
    public void MultiFileBundle_CarriesTheSolutionAndEntryPoint()
    {
        ExecutionEngineSubmission submission = ReferenceSolutionSubmissionFactory.Create(
            new JavaCodeTemplateStrategy(),
            "class Solution { int minCoins(int amount) { return 0; } }",
            "minCoins",
            Inputs(("30", "integer")),
            JavaMultiFileLanguageId
        );

        using MemoryStream stream = new(submission.AdditionalFiles!);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);

        List<string> entries = [.. archive.Entries.Select(entry => entry.FullName)];

        Assert.Contains("Solution.java", entries);
        Assert.Contains("Main.java", entries);
        Assert.Contains("compile", entries);
        Assert.Contains("run", entries);
    }

    [Fact]
    public void SingleFileLanguage_SendsSourceCodeAndNoBundle()
    {
        ExecutionEngineSubmission submission = ReferenceSolutionSubmissionFactory.Create(
            new PythonCodeTemplateStrategy(),
            "def min_coins(amount):\n    return 0",
            "min_coins",
            Inputs(("30", "integer")),
            PythonLanguageId
        );

        Assert.Contains("min_coins", submission.SourceCode);
        Assert.Null(submission.AdditionalFiles);
    }

    [Fact]
    public void ScriptLanguageStdinIsAJsonArrayTheRuntimeSplatsIntoArguments()
    {
        ExecutionEngineSubmission submission = ReferenceSolutionSubmissionFactory.Create(
            new PythonCodeTemplateStrategy(),
            "def solve(a, b):\n    return a",
            "solve",
            Inputs(("[1,2]", "integer_array"), ("3", "integer")),
            PythonLanguageId
        );

        Assert.Equal("[[1,2],3]", submission.Stdin);
    }

    [Fact]
    public void MultiFileStdinIsOneValuePerLineInOrder()
    {
        ExecutionEngineSubmission submission = ReferenceSolutionSubmissionFactory.Create(
            new JavaCodeTemplateStrategy(),
            "class Solution { int solve(int a, int b) { return a; } }",
            "solve",
            Inputs(("4", "integer"), ("3", "integer")),
            JavaMultiFileLanguageId
        );

        Assert.Equal("4\n3", submission.Stdin);
    }

    [Fact]
    public void LanguageIdAndLimitsArePassedThrough()
    {
        ExecutionEngineSubmission submission = ReferenceSolutionSubmissionFactory.Create(
            new PythonCodeTemplateStrategy(),
            "def solve(a):\n    return a",
            "solve",
            Inputs(("1", "integer")),
            PythonLanguageId
        );

        Assert.Equal(PythonLanguageId, submission.LanguageId);
        Assert.Null(submission.TimeLimitMs);
        Assert.Null(submission.MemoryLimitKb);
    }
}