using Moq;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.ValueTypes;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Domain.ValueTypes.ValueObjects;
using StackDuel.Infrastructure.ExecutionEngine;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine;

public class Judge0ReferenceSolutionRunnerTests
{
    private Mock<IExecutionEngineStrategy> _executionEngine = null!;
    private Mock<IValueTypeSpecResolver> _valueTypeSpecResolver = null!;
    private Judge0ReferenceSolutionRunner _runner = null!;
    private List<ExecutionEngineSubmission> _submitted = null!;

    public Judge0ReferenceSolutionRunnerTests()
    {
        _executionEngine = new Mock<IExecutionEngineStrategy>();
        _valueTypeSpecResolver = new Mock<IValueTypeSpecResolver>();
        _submitted = [];

        _valueTypeSpecResolver
            .Setup(x => x.ResolveAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ValueTypeSpec>());

        _executionEngine
            .Setup(x =>
                x.SubmitBatchAsync(
                    It.IsAny<IReadOnlyList<ExecutionEngineSubmission>>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(
                (IReadOnlyList<ExecutionEngineSubmission> submissions, Guid? _, CancellationToken __) =>
                {
                    _submitted.AddRange(submissions);

                    return Task.FromResult<IReadOnlyList<ExecutionEngineResult>>(
                        [.. submissions.Select((_, index) => Accepted($"token-{index}"))]
                    );
                }
            );

        _executionEngine
            .Setup(x => x.PollBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlyList<string> tokens, CancellationToken _) => [.. tokens.Select(Accepted)]
            );

        _runner = new Judge0ReferenceSolutionRunner(_executionEngine.Object, _valueTypeSpecResolver.Object);
    }

    private static IReadOnlyList<TestCase> OneCase()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Sample);
        var testCase = suite.AddTestCase("Case 1");
        testCase.AddInput("9", "integer");
        testCase.AddExpectedOutput("9", "integer");

        return [testCase];
    }

    private static ExecutionEngineResult Accepted(string token) =>
        new(token, "9", null, null, 1, 1, ExecutionEngineResultStatus.Accepted);

    private async Task RunAgainst(ICodeTemplateStrategy strategy)
    {
        await _runner.RunAgainstCasesAsync(
            "reference code",
            "solve",
            strategy,
            judge0LanguageId: 89,
            OneCase(),
            CancellationToken.None
        );
    }

    [Fact]
    public async Task RunAgainstCasesAsync_BundledLanguage_SendsItsAdditionalFiles()
    {
        await RunAgainst(new BundledStrategy());

        Assert.NotEmpty(_submitted);
        Assert.All(_submitted, submission => Assert.NotNull(submission.AdditionalFiles));
        Assert.All(_submitted, submission => Assert.NotEmpty(submission.AdditionalFiles!));
    }

    [Fact]
    public async Task RunAgainstCasesAsync_PlainLanguage_SendsNoAdditionalFiles()
    {
        await RunAgainst(new PlainStrategy());

        Assert.NotEmpty(_submitted);
        Assert.All(_submitted, submission => Assert.Null(submission.AdditionalFiles));
        Assert.All(_submitted, submission => Assert.Equal("rendered", submission.SourceCode));
    }

    private sealed class BundledStrategy : ICodeTemplateStrategy
    {
        public string LanguageName => "Bundled";

        public string Render(CodeTemplateContext context) => string.Empty;

        public byte[]? BuildAdditionalFiles(CodeTemplateContext context) => [1, 2, 3];
    }

    private sealed class PlainStrategy : ICodeTemplateStrategy
    {
        public string LanguageName => "Plain";

        public string Render(CodeTemplateContext context) => "rendered";
    }
}
public class Judge0ReferenceSolutionRunnerCompilationTests
{
    private Mock<IExecutionEngineStrategy> _executionEngine = null!;
    private Mock<IValueTypeSpecResolver> _valueTypeSpecResolver = null!;
    private Judge0ReferenceSolutionRunner _runner = null!;

    public Judge0ReferenceSolutionRunnerCompilationTests()
    {
        _executionEngine = new Mock<IExecutionEngineStrategy>();
        _valueTypeSpecResolver = new Mock<IValueTypeSpecResolver>();

        _valueTypeSpecResolver
            .Setup(x => x.ResolveAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ValueTypeSpec>());

        _runner = new Judge0ReferenceSolutionRunner(_executionEngine.Object, _valueTypeSpecResolver.Object);
    }

    private static IReadOnlyList<TestCase> ThreeCases()
    {
        var suite = new TestSuite("Suite", TestSuiteType.Sample);
        List<TestCase> cases = [];

        for (int i = 1; i <= 3; i++)
        {
            var testCase = suite.AddTestCase($"Example {i}", position: i - 1);
            testCase.AddInput("9", "integer");
            testCase.AddExpectedOutput("9", "integer");
            cases.Add(testCase);
        }

        return cases;
    }

    private void GivenEveryCaseFailsToCompile(string compileOutput)
    {
        ExecutionEngineResult Failed(string token) =>
            new(token, null, null, compileOutput, null, null, ExecutionEngineResultStatus.CompilationError);

        _executionEngine
            .Setup(x =>
                x.SubmitBatchAsync(
                    It.IsAny<IReadOnlyList<ExecutionEngineSubmission>>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyList<ExecutionEngineSubmission> submissions, Guid? _, CancellationToken __) =>
                    [.. submissions.Select((_, index) => Failed($"token-{index}"))]
            );

        _executionEngine
            .Setup(x => x.PollBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> tokens, CancellationToken _) => [.. tokens.Select(Failed)]);
    }

    [Fact]
    public async Task RunAgainstCasesAsync_CompilationError_IsReportedOnceNotPerCase()
    {
        GivenEveryCaseFailsToCompile("Solution.java:5: error: class, interface, enum, or record expected");

        List<string> warnings = await _runner.RunAgainstCasesAsync(
            "reference code",
            "solve",
            new PlainStrategy(),
            judge0LanguageId: 89,
            ThreeCases(),
            CancellationToken.None
        );

        Assert.Single(warnings);
        Assert.Contains("did not compile", warnings[0]);
        Assert.Contains("class, interface, enum, or record expected", warnings[0]);
    }

    private sealed class PlainStrategy : ICodeTemplateStrategy
    {
        public string LanguageName => "Plain";

        public string Render(CodeTemplateContext context) => "rendered";
    }
}