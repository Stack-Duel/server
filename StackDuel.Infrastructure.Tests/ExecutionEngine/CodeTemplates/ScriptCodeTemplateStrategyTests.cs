using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

/// <summary>
/// The script languages (Python/JavaScript/TypeScript) all wrap the submitted code in a
/// stdin-reading harness, so they share one set of expectations.
/// </summary>
public class ScriptCodeTemplateStrategyTests
{
    public static TheoryData<ICodeTemplateStrategy, string> Strategies =>
        new()
        {
            { new PythonCodeTemplateStrategy(), "Python" },
            { new JavaScriptCodeTemplateStrategy(), "JavaScript" },
            { new TypeScriptCodeTemplateStrategy(), "TypeScript" },
        };

    private static CodeTemplateContext Context(
        string userCode = "def solve(a): return a",
        string? functionName = "solve"
    ) => new(userCode, functionName, []);

    [Theory]
    [MemberData(nameof(Strategies))]
    public void LanguageName_IsTheLanguageItRenders(ICodeTemplateStrategy strategy, string expected)
    {
        Assert.Equal(expected, strategy.LanguageName);
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Render_EmbedsTheSubmittedCode(ICodeTemplateStrategy strategy, string languageName)
    {
        _ = languageName;

        string rendered = strategy.Render(Context(userCode: "// MARKER_USER_CODE"));

        Assert.Contains("// MARKER_USER_CODE", rendered);
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Render_CallsTheEntryPointFunction(ICodeTemplateStrategy strategy, string languageName)
    {
        _ = languageName;

        string rendered = strategy.Render(Context(functionName: "twoSum"));

        Assert.Contains("twoSum", rendered);
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Render_ReadsArgumentsFromStdin(ICodeTemplateStrategy strategy, string languageName)
    {
        _ = languageName;

        string rendered = strategy.Render(Context());

        Assert.Contains("stdin", rendered);
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void BuildAdditionalFiles_IsNullForScriptLanguages(ICodeTemplateStrategy strategy, string languageName)
    {
        _ = languageName;

        Assert.Null(strategy.BuildAdditionalFiles(Context()));
    }

    [Fact]
    public void Render_Python_SerializesNonStringResultsAsJson()
    {
        string rendered = new PythonCodeTemplateStrategy().Render(Context());

        Assert.Contains("json.dumps(result)", rendered);
        Assert.Contains("isinstance(result, str)", rendered);
    }

    [Fact]
    public void Render_JavaScript_SerializesNonStringResultsAsJson()
    {
        string rendered = new JavaScriptCodeTemplateStrategy().Render(Context());

        Assert.Contains("JSON.stringify(result)", rendered);
        Assert.Contains("typeof result === \"string\"", rendered);
    }

    [Fact]
    public void Render_TypeScript_DeclaresProcessSoTheHarnessCompiles()
    {
        string rendered = new TypeScriptCodeTemplateStrategy().Render(Context());

        Assert.Contains("declare const process", rendered);
    }
}