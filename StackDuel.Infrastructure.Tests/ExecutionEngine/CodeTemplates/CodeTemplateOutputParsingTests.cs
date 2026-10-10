using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;
using System.Text.Json;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class CodeTemplateOutputParsingTests
{
    private static readonly ICodeTemplateStrategy Strategy = new PythonCodeTemplateStrategy();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ParseOutput_NoOutput_YieldsNothing(string? stdout)
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput(stdout);

        Assert.Null(parsed.UserLogs);
        Assert.Null(parsed.ActualResult);
    }

    [Fact]
    public void ParseOutput_SingleLine_IsTheResultWithNoLogs()
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput("[0,1]");

        Assert.Null(parsed.UserLogs);
        Assert.Equal("[0,1]", parsed.ActualResult);
    }

    [Fact]
    public void ParseOutput_LastLineIsTheResultAndEarlierLinesAreUserLogs()
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput("debug one\ndebug two\n[0,1]");

        Assert.Equal("debug one,debug two", parsed.UserLogs);
        Assert.Equal("[0,1]", parsed.ActualResult);
    }

    [Fact]
    public void ParseOutput_StripsASingleTrailingNewline()
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput("[0,1]\n");

        Assert.Null(parsed.UserLogs);
        Assert.Equal("[0,1]", parsed.ActualResult);
    }

    [Theory]
    [InlineData("log\r\nresult", "log", "result")]
    [InlineData("log\rresult", "log", "result")]
    [InlineData("log\nresult", "log", "result")]
    public void ParseOutput_HandlesEveryLineEndingStyle(string stdout, string expectedLogs, string expectedResult)
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput(stdout);

        Assert.Equal(expectedLogs, parsed.UserLogs);
        Assert.Equal(expectedResult, parsed.ActualResult);
    }

    [Fact]
    public void ParseOutput_BlankLastLine_LeavesTheResultNullButKeepsTheLogs()
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput("only a log\n   ");

        Assert.Equal("only a log", parsed.UserLogs);
        Assert.Null(parsed.ActualResult);
    }

    [Fact]
    public void BuildStdin_SerializesInputsAsAJsonArrayOfArguments()
    {
        string stdin = Strategy.BuildStdin([
            new CodeTemplateInput("[2,7,11]", "integer_array"),
            new CodeTemplateInput("9", "integer"),
        ]);

        Assert.Equal("[[2,7,11],9]", stdin);
    }

    [Fact]
    public void BuildStdin_NoInputs_IsAnEmptyJsonArray()
    {
        Assert.Equal("[]", Strategy.BuildStdin([]));
    }

    [Fact]
    public void BuildStdin_NonJsonValue_IsQuotedAsAJsonString()
    {
        string stdin = Strategy.BuildStdin([new CodeTemplateInput("hello world", "string")]);

        Assert.Equal("[\"hello world\"]", stdin);
    }

    [Fact]
    public void BuildStdin_AlreadyQuotedString_IsNotDoubleQuoted()
    {
        string stdin = Strategy.BuildStdin([new CodeTemplateInput("\"hello\"", "string")]);

        Assert.Equal("[\"hello\"]", stdin);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("3.5")]
    [InlineData("{\"a\":1}")]
    public void BuildStdin_PreservesJsonShapedValues(string value)
    {
        string stdin = Strategy.BuildStdin([new CodeTemplateInput(value, "any")]);

        using JsonDocument document = JsonDocument.Parse(stdin);
        Assert.Equal(value, document.RootElement[0].GetRawText());
    }
}

public class SQLiteCodeTemplateStrategyTests
{
    private static readonly SQLiteCodeTemplateStrategy Strategy = new();

    [Fact]
    public void LanguageName_IsSQLite()
    {
        Assert.Equal("SQLite", Strategy.LanguageName);
    }

    [Fact]
    public void Render_EmitsTheDotCommandsThatMakeOutputComparable()
    {
        string rendered = Strategy.Render(new CodeTemplateContext("SELECT 1;", null, []));

        Assert.Contains(".mode list", rendered);
        Assert.Contains(".headers off", rendered);
        Assert.Contains(".separator \",\"", rendered);
        Assert.Contains(".nullvalue NULL", rendered);
    }

    [Fact]
    public void Render_IncludesTheFirstInputAsSchemaSetupBeforeTheQuery()
    {
        string rendered = Strategy.Render(
            new CodeTemplateContext("SELECT * FROM t;", null, [new CodeTemplateInput("CREATE TABLE t(a);", "sql")])
        );

        Assert.True(
            rendered.IndexOf("CREATE TABLE t(a);", StringComparison.Ordinal)
                < rendered.IndexOf("SELECT * FROM t;", StringComparison.Ordinal),
            "setup SQL must run before the submitted query"
        );
    }

    [Fact]
    public void Render_NoInputs_StillRendersTheQuery()
    {
        string rendered = Strategy.Render(new CodeTemplateContext("SELECT 1;", null, []));

        Assert.Contains("SELECT 1;", rendered);
    }

    [Fact]
    public void Render_TrimsTheSubmittedQuery()
    {
        string rendered = Strategy.Render(new CodeTemplateContext("   SELECT 1;   ", null, []));

        Assert.EndsWith("SELECT 1;", rendered);
    }

    [Fact]
    public void Render_IgnoresInputsBeyondTheFirst()
    {
        string rendered = Strategy.Render(
            new CodeTemplateContext(
                "SELECT 1;",
                null,
                [new CodeTemplateInput("CREATE TABLE a(x);", "sql"), new CodeTemplateInput("SHOULD_NOT_APPEAR", "sql")]
            )
        );

        Assert.DoesNotContain("SHOULD_NOT_APPEAR", rendered);
    }

    [Fact]
    public void BuildStdin_IsEmptyBecauseTheSchemaIsInlinedIntoTheScript()
    {
        Assert.Equal(string.Empty, Strategy.BuildStdin([new CodeTemplateInput("CREATE TABLE t(a);", "sql")]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("   ")]
    public void ParseOutput_BlankOutput_YieldsNoResult(string? stdout)
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput(stdout);

        Assert.Null(parsed.UserLogs);
        Assert.Null(parsed.ActualResult);
    }

    [Fact]
    public void ParseOutput_KeepsEveryRowAsTheResultRatherThanTreatingEarlierRowsAsLogs()
    {
        ParsedExecutionOutput parsed = Strategy.ParseOutput("row1\nrow2\nrow3\n");

        Assert.Null(parsed.UserLogs);
        Assert.Equal("row1\nrow2\nrow3", parsed.ActualResult);
    }

    [Fact]
    public void ParseOutput_SingleRow_IsTheResult()
    {
        Assert.Equal("42", Strategy.ParseOutput("42\n").ActualResult);
    }
}