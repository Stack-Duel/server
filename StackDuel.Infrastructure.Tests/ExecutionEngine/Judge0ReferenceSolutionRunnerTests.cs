using StackDuel.Application.ExecutionEngine;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using StackDuel.Infrastructure.ExecutionEngine;
using StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

namespace StackDuel.Infrastructure.Tests.ExecutionEngine;

public class Judge0ReferenceSolutionRunnerTests
{
    private const int MultiFileJudge0LanguageId = 89;

    private sealed class SubmissionCapturingEngine : IExecutionEngineStrategy
    {
        public List<ExecutionEngineSubmission> Captured { get; } = [];

        public Task<IReadOnlyList<ExecutionEngineResult>> SubmitBatchAsync(
            IReadOnlyList<ExecutionEngineSubmission> submissions,
            Guid? submissionId = null,
            CancellationToken cancellationToken = default
        )
        {
            Captured.AddRange(submissions);

            IReadOnlyList<ExecutionEngineResult> results =
            [
                .. submissions.Select(
                    (_, index) =>
                        new ExecutionEngineResult(
                            Token: $"token-{index}",
                            Stdout: "0",
                            Stderr: null,
                            CompileOutput: null,
                            RuntimeMs: 1,
                            MemoryUsedKb: 1,
                            Status: ExecutionEngineResultStatus.Accepted
                        )
                ),
            ];

            return Task.FromResult(results);
        }

        public Task<IReadOnlyList<ExecutionEngineResult>> PollBatchAsync(
            IReadOnlyList<string> tokens,
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<ExecutionEngineResult> results =
            [
                .. tokens.Select(token => new ExecutionEngineResult(
                    Token: token,
                    Stdout: "0",
                    Stderr: null,
                    CompileOutput: null,
                    RuntimeMs: 1,
                    MemoryUsedKb: 1,
                    Status: ExecutionEngineResultStatus.Accepted
                )),
            ];

            return Task.FromResult(results);
        }
    }

    private static List<TestCase> SampleCases(int count)
    {
        TestSuite suite = new("samples", TestSuiteType.Sample);
        List<TestCase> cases = [];

        for (int index = 0; index < count; index++)
        {
            TestCase testCase = suite.AddTestCase($"case-{index}");
            testCase.AddInput(index.ToString(), "integer");
            testCase.AddExpectedOutput("0", "integer");
            cases.Add(testCase);
        }

        return cases;
    }

    [Fact]
    public async Task EveryMultiFileSubmissionCarriesTheFileBundle()
    {
        SubmissionCapturingEngine engine = new();
        Judge0ReferenceSolutionRunner runner = new(engine);

        await runner.RunAgainstCasesAsync(
            referenceSolutionCode: "class Solution { int minCoins(int amount) { return 0; } }",
            functionName: "minCoins",
            templateStrategy: new JavaCodeTemplateStrategy(),
            judge0LanguageId: MultiFileJudge0LanguageId,
            testCases: SampleCases(4),
            cancellationToken: CancellationToken.None
        );

        Assert.Equal(4, engine.Captured.Count);
        Assert.All(engine.Captured, submission => Assert.NotNull(submission.AdditionalFiles));
        Assert.All(engine.Captured, submission => Assert.NotEmpty(submission.AdditionalFiles!));
    }

    [Fact]
    public async Task SingleFileSubmissionsSendSourceCodeAndNoBundle()
    {
        SubmissionCapturingEngine engine = new();
        Judge0ReferenceSolutionRunner runner = new(engine);

        await runner.RunAgainstCasesAsync(
            referenceSolutionCode: "def min_coins(amount):\n    return 0",
            functionName: "min_coins",
            templateStrategy: new PythonCodeTemplateStrategy(),
            judge0LanguageId: 109,
            testCases: SampleCases(2),
            cancellationToken: CancellationToken.None
        );

        Assert.Equal(2, engine.Captured.Count);
        Assert.All(engine.Captured, submission => Assert.NotEmpty(submission.SourceCode));
        Assert.All(engine.Captured, submission => Assert.Null(submission.AdditionalFiles));
    }
}