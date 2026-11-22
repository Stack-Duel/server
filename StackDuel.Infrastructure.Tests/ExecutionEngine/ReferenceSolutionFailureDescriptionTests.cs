using StackDuel.Application.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine;

public class ReferenceSolutionFailureDescriptionTests
{
    private const string NodeSyntaxErrorStderr = """
        (node:13) [MODULE_TYPELESS_PACKAGE_JSON] Warning: file:///box/script.js parsed as an ES module because module syntax was detected; to avoid the performance penalty of syntax detection, add "type": "module" to /package.json
        (Use `node --trace-warnings ...` to show where the warning was created)
        file:///box/script.js:2
            const vowels = includeY ? "aeiouy" : "aeiou";
            ^^^^^

        SyntaxError: Unexpected token 'const'
            at compileSourceTextModule (node:internal/modules/esm/utils:337:16)
            at ModuleLoader.moduleStrategy (node:internal/modules/esm/translators:102:18)
        """;

    private static ExecutionEngineResult Result(
        ExecutionEngineResultStatus status,
        string? stderr = null,
        string? compileOutput = null,
        string? stdout = null
    ) => new("token", stdout, stderr, compileOutput, null, null, status);

    [Fact]
    public void Describe_KeepsTheErrorLineAndDropsNodeNoise()
    {
        string description = ReferenceSolutionFailureDescription.Describe(
            "Y counts when asked",
            Result(ExecutionEngineResultStatus.RuntimeError, stderr: NodeSyntaxErrorStderr),
            expected: "1",
            actual: null
        );

        Assert.Equal(
            "Case 'Y counts when asked': runtime error. Expected 1, got no output. "
                + "SyntaxError: Unexpected token 'const'",
            description
        );
        Assert.DoesNotContain("MODULE_TYPELESS_PACKAGE_JSON", description);
        Assert.DoesNotContain("trace-warnings", description);
        Assert.DoesNotContain("    at ", description);
    }

    [Fact]
    public void Describe_StaysShortEnoughToStore()
    {
        string description = ReferenceSolutionFailureDescription.Describe(
            "Huge failure",
            Result(ExecutionEngineResultStatus.RuntimeError, stderr: new string('x', 20_000)),
            expected: new string('e', 5_000),
            actual: new string('a', 5_000)
        );

        Assert.True(description.Length < 1_000, $"Description was {description.Length} characters long.");
    }

    [Fact]
    public void Describe_WrongAnswer_ReportsExpectedAndActual()
    {
        string description = ReferenceSolutionFailureDescription.Describe(
            "Mixed vowels",
            Result(ExecutionEngineResultStatus.WrongAnswer, stdout: "4"),
            expected: "3",
            actual: "4"
        );

        Assert.Equal("Case 'Mixed vowels': wrong answer. Expected 3, got 4.", description);
    }

    [Fact]
    public void Describe_PrefersCompileOutputOverStderr()
    {
        string description = ReferenceSolutionFailureDescription.Describe(
            "Any case",
            Result(
                ExecutionEngineResultStatus.CompilationError,
                stderr: "ignored runtime noise",
                compileOutput: "Main.java:3: error: ';' expected"
            ),
            expected: "1",
            actual: null
        );

        Assert.Contains("compile error", description);
        Assert.Contains("Main.java:3: error: ';' expected", description);
        Assert.DoesNotContain("ignored runtime noise", description);
    }

    [Fact]
    public void Describe_MissingResult_SaysSo()
    {
        string description = ReferenceSolutionFailureDescription.Describe(
            "Never ran",
            result: null,
            expected: "1",
            actual: null
        );

        Assert.Equal("Case 'Never ran': no result. Expected 1, got no output.", description);
    }

    [Fact]
    public void Describe_StderrWithoutAnErrorLine_FallsBackToTheFirstUsefulLine()
    {
        string description = ReferenceSolutionFailureDescription.Describe(
            "Odd case",
            Result(ExecutionEngineResultStatus.RuntimeError, stderr: "(node:9) something\nkilled by signal 9"),
            expected: "1",
            actual: null
        );

        Assert.Contains("killed by signal 9", description);
    }
}