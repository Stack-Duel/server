using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.Text.Json;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class VitestJavaScriptBatchTemplateStrategyTests
{
    private const string ActualOutputsMarker = "===ACTUAL_OUTPUTS===";
    private const string VitestReportMarker = "===VITEST_REPORT===";

    private static readonly VitestJavaScriptBatchTemplateStrategy Strategy = new();

    private static BatchTestCase Case(
        Guid id,
        string expected = "[0,1]",
        BatchAssertStrategy assertStrategy = BatchAssertStrategy.ExactMatch,
        decimal? tolerance = null,
        bool caseSensitive = true
    ) => new(id, [new CodeTemplateInput("[2,7]", "integer_array")], expected, assertStrategy, tolerance, caseSensitive);

    /// <summary>Builds the stdout shape the Judge0 run command produces for this strategy.</summary>
    private static string Stdout(object? outputs, object report)
    {
        string outputsJson = outputs is null ? "" : JsonSerializer.Serialize(outputs);
        return $"{ActualOutputsMarker}\n{outputsJson}\n{VitestReportMarker}\n{JsonSerializer.Serialize(report)}";
    }

    private static object Report(
        params (string Title, string Status, string[] FailureMessages, int? Duration)[] assertions
    ) =>
        new
        {
            testResults = new[]
            {
                new
                {
                    assertionResults = assertions
                        .Select(a => new
                        {
                            title = a.Title,
                            status = a.Status,
                            failureMessages = a.FailureMessages,
                            duration = a.Duration,
                        })
                        .ToArray(),
                },
            },
        };

    [Fact]
    public void LanguageName_IsJavaScript()
    {
        Assert.Equal("JavaScript", Strategy.LanguageName);
    }

    [Fact]
    public void RenderBatch_EmbedsTheSubmittedCodeAndEntryPoint()
    {
        string rendered = Strategy.RenderBatch("// MARKER", "twoSum", [Case(Guid.NewGuid())], 2000);

        Assert.Contains("// MARKER", rendered);
        Assert.Contains("twoSum(...c.args)", rendered);
    }

    [Fact]
    public void RenderBatch_AppliesThePerCaseTimeout()
    {
        string rendered = Strategy.RenderBatch("", "solve", [Case(Guid.NewGuid())], 1234);

        Assert.Contains("timeout: 1234", rendered);
    }

    [Fact]
    public void RenderBatch_EmitsOneHarnessCasePerTestCaseKeyedById()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        string rendered = Strategy.RenderBatch("", "solve", [Case(first), Case(second)], 2000);

        Assert.Contains(first.ToString(), rendered);
        Assert.Contains(second.ToString(), rendered);
    }

    [Fact]
    public void RenderBatch_CarriesEachCasesAssertStrategyIntoTheHarness()
    {
        string rendered = Strategy.RenderBatch(
            "",
            "solve",
            [Case(Guid.NewGuid(), assertStrategy: BatchAssertStrategy.FloatTolerance, tolerance: 0.5m)],
            2000
        );

        Assert.Contains("\"strategy\":\"FloatTolerance\"", rendered);
        Assert.Contains("\"tolerance\":0.5", rendered);
    }

    [Fact]
    public void RenderBatch_CarriesCaseSensitivity()
    {
        string rendered = Strategy.RenderBatch("", "solve", [Case(Guid.NewGuid(), caseSensitive: false)], 2000);

        Assert.Contains("\"caseSensitive\":false", rendered);
    }

    [Fact]
    public void RenderBatch_JsonParsesInputsSoArraysArriveAsArrays()
    {
        string rendered = Strategy.RenderBatch("", "solve", [Case(Guid.NewGuid())], 2000);

        Assert.Contains("\"args\":[[2,7]]", rendered);
    }

    [Fact]
    public void RenderBatch_NonJsonInput_IsPassedAsAString()
    {
        BatchTestCase stringCase = new(
            Guid.NewGuid(),
            [new CodeTemplateInput("hello", "string")],
            "olleh",
            BatchAssertStrategy.ExactMatch
        );

        string rendered = Strategy.RenderBatch("", "reverse", [stringCase], 2000);

        Assert.Contains("\"args\":[\"hello\"]", rendered);
    }

    [Fact]
    public void RenderBatch_PrintsTheActualOutputsBlockSoResultsCanBeRecovered()
    {
        string rendered = Strategy.RenderBatch("", "solve", [Case(Guid.NewGuid())], 2000);

        Assert.Contains(ActualOutputsMarker, rendered);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseBatchOutput_NoOutput_IsARuntimeErrorForEveryCase(string? stdout)
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        IReadOnlyList<BatchCaseResult> results = Strategy.ParseBatchOutput(stdout, [first, second]);

        Assert.Equal([first, second], results.Select(r => r.TestCaseId));
        Assert.All(results, r => Assert.Equal(BatchCaseStatus.RuntimeError, r.Status));
        Assert.All(results, r => Assert.Equal("No output from Vitest.", r.ErrorMessage));
    }

    [Fact]
    public void ParseBatchOutput_UnparseableReport_IsARuntimeErrorForEveryCase()
    {
        Guid id = Guid.NewGuid();

        IReadOnlyList<BatchCaseResult> results = Strategy.ParseBatchOutput("not json at all", [id]);

        Assert.Equal(BatchCaseStatus.RuntimeError, results.Single().Status);
        Assert.Equal("Could not parse Vitest report.", results.Single().ErrorMessage);
    }

    [Fact]
    public void ParseBatchOutput_PassedCase_IsAcceptedWithItsActualOutputAndDuration()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(
            new Dictionary<string, string> { [id.ToString()] = "[0,1]" },
            Report((id.ToString(), "passed", [], 12))
        );

        BatchCaseResult result = Strategy.ParseBatchOutput(stdout, [id]).Single();

        Assert.Equal(BatchCaseStatus.Accepted, result.Status);
        Assert.Equal("[0,1]", result.ActualOutput);
        Assert.Equal(12, result.RuntimeMs);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ParseBatchOutput_AssertionFailure_IsWrongAnswer()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(
            new Dictionary<string, string> { [id.ToString()] = "[1,0]" },
            Report((id.ToString(), "failed", ["AssertionError: expected [1,0] to be [0,1]"], 5))
        );

        BatchCaseResult result = Strategy.ParseBatchOutput(stdout, [id]).Single();

        Assert.Equal(BatchCaseStatus.WrongAnswer, result.Status);
        Assert.Equal("[1,0]", result.ActualOutput);
        Assert.Contains("AssertionError", result.ErrorMessage);
    }

    [Fact]
    public void ParseBatchOutput_TimeoutFailure_IsTimedOut()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(null, Report((id.ToString(), "failed", ["Test timed out in 2000ms."], null)));

        Assert.Equal(BatchCaseStatus.TimedOut, Strategy.ParseBatchOutput(stdout, [id]).Single().Status);
    }

    [Fact]
    public void ParseBatchOutput_TimeoutWinsOverTheAssertionWording()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(null, Report((id.ToString(), "failed", ["expected result but test timed out"], null)));

        Assert.Equal(BatchCaseStatus.TimedOut, Strategy.ParseBatchOutput(stdout, [id]).Single().Status);
    }

    [Fact]
    public void ParseBatchOutput_ThrownError_IsARuntimeError()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(null, Report((id.ToString(), "failed", ["TypeError: x is not a function"], null)));

        Assert.Equal(BatchCaseStatus.RuntimeError, Strategy.ParseBatchOutput(stdout, [id]).Single().Status);
    }

    [Fact]
    public void ParseBatchOutput_FailureWithNoMessage_IsARuntimeError()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(null, Report((id.ToString(), "failed", [], null)));

        Assert.Equal(BatchCaseStatus.RuntimeError, Strategy.ParseBatchOutput(stdout, [id]).Single().Status);
    }

    [Fact]
    public void ParseBatchOutput_UnknownStatus_IsARuntimeError()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(null, Report((id.ToString(), "skipped", [], null)));

        Assert.Equal(BatchCaseStatus.RuntimeError, Strategy.ParseBatchOutput(stdout, [id]).Single().Status);
    }

    [Fact]
    public void ParseBatchOutput_CaseMissingFromTheReport_IsARuntimeError()
    {
        Guid reported = Guid.NewGuid();
        Guid missing = Guid.NewGuid();
        string stdout = Stdout(null, Report((reported.ToString(), "passed", [], 1)));

        IReadOnlyList<BatchCaseResult> results = Strategy.ParseBatchOutput(stdout, [reported, missing]);

        Assert.Equal(BatchCaseStatus.Accepted, results[0].Status);
        Assert.Equal(BatchCaseStatus.RuntimeError, results[1].Status);
        Assert.Equal("No matching Vitest result for this case.", results[1].ErrorMessage);
    }

    [Fact]
    public void ParseBatchOutput_ReturnsResultsInTheRequestedOrderNotTheReportedOne()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        string stdout = Stdout(null, Report((second.ToString(), "passed", [], 1), (first.ToString(), "passed", [], 2)));

        IReadOnlyList<BatchCaseResult> results = Strategy.ParseBatchOutput(stdout, [first, second]);

        Assert.Equal([first, second], results.Select(r => r.TestCaseId));
    }

    [Fact]
    public void ParseBatchOutput_MissingOutputsMarker_ReportsAParseFailureRatherThanMisreadingTheStream()
    {
        // Without both markers the split can't be trusted, so the whole stream is treated as the
        // report — which then fails to parse, surfacing a clear error instead of a bogus verdict.
        Guid id = Guid.NewGuid();
        string stdout = $"{VitestReportMarker}\n{JsonSerializer.Serialize(Report((id.ToString(), "passed", [], 1)))}";

        BatchCaseResult result = Strategy.ParseBatchOutput(stdout, [id]).Single();

        Assert.Equal(BatchCaseStatus.RuntimeError, result.Status);
        Assert.Equal("Could not parse Vitest report.", result.ErrorMessage);
        Assert.Null(result.ActualOutput);
    }

    [Fact]
    public void ParseBatchOutput_MarkersInTheWrongOrder_FallsBackToReadingTheWholeStreamAsTheReport()
    {
        Guid id = Guid.NewGuid();
        string stdout = $"{VitestReportMarker}\n{ActualOutputsMarker}\n{{}}";

        BatchCaseResult result = Strategy.ParseBatchOutput(stdout, [id]).Single();

        Assert.Equal(BatchCaseStatus.RuntimeError, result.Status);
        Assert.Equal("Could not parse Vitest report.", result.ErrorMessage);
    }

    [Fact]
    public void ParseBatchOutput_UnparseableOutputsBlock_StillGradesFromTheReport()
    {
        Guid id = Guid.NewGuid();
        string stdout =
            $"{ActualOutputsMarker}\nnot json\n{VitestReportMarker}\n{JsonSerializer.Serialize(Report((id.ToString(), "passed", [], 1)))}";

        BatchCaseResult result = Strategy.ParseBatchOutput(stdout, [id]).Single();

        Assert.Equal(BatchCaseStatus.Accepted, result.Status);
        Assert.Null(result.ActualOutput);
    }

    [Fact]
    public void ParseBatchOutput_DuplicateReportEntries_UseTheFirstOne()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(
            null,
            Report((id.ToString(), "passed", [], 1), (id.ToString(), "failed", ["AssertionError"], 2))
        );

        Assert.Equal(BatchCaseStatus.Accepted, Strategy.ParseBatchOutput(stdout, [id]).Single().Status);
    }

    [Fact]
    public void ParseBatchOutput_ReportWithNoTestResults_IsARuntimeErrorForEveryCase()
    {
        Guid id = Guid.NewGuid();
        string stdout = Stdout(null, new { });

        Assert.Equal(BatchCaseStatus.RuntimeError, Strategy.ParseBatchOutput(stdout, [id]).Single().Status);
    }
}

public class CodeTemplateStrategyResolverTests
{
    [Fact]
    public void Resolve_ReturnsTheStrategyForTheLanguage()
    {
        PythonCodeTemplateStrategy python = new();
        CodeTemplateStrategyResolver resolver = new([new JavaScriptCodeTemplateStrategy(), python]);

        Assert.Same(python, resolver.Resolve("Python"));
    }

    [Theory]
    [InlineData("python")]
    [InlineData("PYTHON")]
    [InlineData("PyThOn")]
    public void Resolve_MatchesTheLanguageNameCaseInsensitively(string languageName)
    {
        CodeTemplateStrategyResolver resolver = new([new PythonCodeTemplateStrategy()]);

        Assert.Equal("Python", resolver.Resolve(languageName).LanguageName);
    }

    [Fact]
    public void Resolve_UnregisteredLanguage_Throws()
    {
        CodeTemplateStrategyResolver resolver = new([new PythonCodeTemplateStrategy()]);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Rust"));
        Assert.Contains("Rust", exception.Message);
    }

    [Fact]
    public void Resolve_NoStrategiesRegistered_Throws()
    {
        CodeTemplateStrategyResolver resolver = new([]);

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve("Python"));
    }

    [Fact]
    public void Resolve_SeveralStrategiesForTheSameLanguage_PicksTheFirstRegistered()
    {
        JavaScriptCodeTemplateStrategy first = new();
        CodeTemplateStrategyResolver resolver = new([first, new JavaScriptCodeTemplateStrategy()]);

        Assert.Same(first, resolver.Resolve("JavaScript"));
    }
}

public class BatchCodeTemplateStrategyResolverTests
{
    [Fact]
    public void Resolve_ReturnsTheBatchStrategyForTheLanguage()
    {
        VitestJavaScriptBatchTemplateStrategy vitest = new();
        BatchCodeTemplateStrategyResolver resolver = new([vitest]);

        Assert.Same(vitest, resolver.Resolve("JavaScript"));
    }

    [Fact]
    public void Resolve_MatchesTheLanguageNameCaseInsensitively()
    {
        BatchCodeTemplateStrategyResolver resolver = new([new VitestJavaScriptBatchTemplateStrategy()]);

        Assert.Equal("JavaScript", resolver.Resolve("javascript").LanguageName);
    }

    [Fact]
    public void Resolve_UnregisteredLanguage_Throws()
    {
        BatchCodeTemplateStrategyResolver resolver = new([new VitestJavaScriptBatchTemplateStrategy()]);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.Resolve("Python")
        );
        Assert.Contains("Python", exception.Message);
    }
}