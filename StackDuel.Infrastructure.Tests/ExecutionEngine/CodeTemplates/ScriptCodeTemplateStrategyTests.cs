using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine.CodeTemplates;

public class ScriptCodeTemplateStrategyTests
{
    public static TheoryData<string> ScriptLanguages => ["Python", "JavaScript", "TypeScript"];

    private static ICodeTemplateStrategy StrategyFor(string languageName) =>
        languageName switch
        {
            "Python" => new PythonCodeTemplateStrategy(),
            "JavaScript" => new JavaScriptCodeTemplateStrategy(),
            "TypeScript" => new TypeScriptCodeTemplateStrategy(),
            _ => throw new ArgumentOutOfRangeException(nameof(languageName), languageName, null),
        };

    private static CodeTemplateContext Context(
        string userCode = "def solve(a): return a",
        string? functionName = "solve"
    ) => new(userCode, functionName, []);

    [Theory]
    [MemberData(nameof(ScriptLanguages))]
    public void LanguageName_IsTheLanguageItRenders(string languageName)
    {
        Assert.Equal(languageName, StrategyFor(languageName).LanguageName);
    }

    [Theory]
    [MemberData(nameof(ScriptLanguages))]
    public void Render_EmbedsTheSubmittedCode(string languageName)
    {
        string rendered = StrategyFor(languageName).Render(Context(userCode: "// MARKER_USER_CODE"));

        Assert.Contains("// MARKER_USER_CODE", rendered);
    }

    [Theory]
    [MemberData(nameof(ScriptLanguages))]
    public void Render_CallsTheEntryPointFunction(string languageName)
    {
        string rendered = StrategyFor(languageName).Render(Context(functionName: "twoSum"));

        Assert.Contains("twoSum", rendered);
    }

    [Theory]
    [MemberData(nameof(ScriptLanguages))]
    public void Render_ReadsArgumentsFromStdin(string languageName)
    {
        string rendered = StrategyFor(languageName).Render(Context());

        Assert.Contains("stdin", rendered);
    }

    [Theory]
    [MemberData(nameof(ScriptLanguages))]
    public void BuildAdditionalFiles_IsNullForScriptLanguages(string languageName)
    {
        Assert.Null(StrategyFor(languageName).BuildAdditionalFiles(Context()));
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