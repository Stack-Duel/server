using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.ValueTypes;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.ValueTypes.ValueObjects;
using System.Text.Json;

namespace StackDuel.Infrastructure.ExecutionEngine;

internal sealed class Judge0ReferenceSolutionRunner(
    IExecutionEngineStrategy executionEngine,
    IValueTypeSpecResolver valueTypeSpecResolver
)
{
    private const int MaxPollAttempts = 30;
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(2);

    public async Task<List<string>> RunAgainstCasesAsync(
        string referenceSolutionCode,
        string? functionName,
        ICodeTemplateStrategy templateStrategy,
        int judge0LanguageId,
        IReadOnlyList<TestCase> testCases,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyDictionary<string, ValueTypeSpec> specsByKey = await valueTypeSpecResolver.ResolveAsync(
            testCases.SelectMany(tc => tc.Inputs.Select(i => i.ValueType)).Distinct(),
            cancellationToken
        );

        List<List<CodeTemplateInput>> inputsByCase =
        [
            .. testCases.Select(tc =>
                tc.Inputs
                    .Select(i => new CodeTemplateInput(
                        i.Value,
                        i.ValueType,
                        specsByKey.GetValueOrDefault(i.ValueType)
                    ))
                    .ToList()
            ),
        ];

        List<ExecutionEngineSubmission> submissions = [.. inputsByCase.Select(BuildSubmission)];

        ExecutionEngineSubmission BuildSubmission(List<CodeTemplateInput> inputs)
        {
            CodeTemplateContext context = new(referenceSolutionCode, functionName, inputs);

            return new ExecutionEngineSubmission(
                SourceCode: templateStrategy.Render(context),
                LanguageId: judge0LanguageId,
                Stdin: templateStrategy.BuildStdin(inputs),
                TimeLimitMs: null,
                MemoryLimitKb: null,
                AdditionalFiles: templateStrategy.BuildAdditionalFiles(context)
            );
        }

        List<ExecutionEngineResult> results = await SubmitAndPollAsync(submissions, cancellationToken);

        ExecutionEngineResult? compilationFailure = results.FirstOrDefault(r =>
            r.Status == ExecutionEngineResultStatus.CompilationError
        );

        if (compilationFailure is not null)
            return [$"Reference solution did not compile: {compilationFailure.CompileOutput?.Trim()}"];

        List<string> warnings = [];
        for (int i = 0; i < testCases.Count; i++)
        {
            ExecutionEngineResult? result = results.Count > i ? results[i] : null;
            string? expected = testCases[i].ExpectedOutputs.OrderBy(e => e.Id).Select(e => e.Value).FirstOrDefault();
            string? actual = result is null ? null : ParseActualOutput(result.Stdout);

            bool ok =
                result is not null
                && result.Status == ExecutionEngineResultStatus.Accepted
                && expected is not null
                && actual is not null
                && ValuesMatch(actual, expected);

            if (!ok)
            {
                warnings.Add(
                    $"Reference solution failed on case '{testCases[i].Name}' "
                        + $"(status: {result?.Status.ToString() ?? "none"}, expected: {expected}, actual: {actual}, "
                        + $"stderr: {result?.Stderr}, compileOutput: {result?.CompileOutput})."
                );
            }
        }

        return warnings;
    }

    public async Task<List<ExecutionEngineResult>> SubmitAndPollAsync(
        List<ExecutionEngineSubmission> submissions,
        CancellationToken cancellationToken
    )
    {
        if (submissions.Count == 0)
            return [];

        IReadOnlyList<ExecutionEngineResult> submitted = await executionEngine.SubmitBatchAsync(
            submissions,
            cancellationToken: cancellationToken
        );
        List<string> tokens = [.. submitted.Select(r => r.Token)];

        Dictionary<string, ExecutionEngineResult> latest = submitted.ToDictionary(r => r.Token);

        for (int attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            IReadOnlyList<ExecutionEngineResult> polled = await executionEngine.PollBatchAsync(
                tokens,
                cancellationToken
            );
            foreach (ExecutionEngineResult result in polled)
                latest[result.Token] = result;

            bool allDone = latest.Values.All(r =>
                r.Status != ExecutionEngineResultStatus.Queued && r.Status != ExecutionEngineResultStatus.Processing
            );

            if (allDone)
                break;

            await Task.Delay(PollDelay, cancellationToken);
        }

        return [.. tokens.Select(t => latest[t])];
    }

    public static bool ValuesMatch(string actual, string expected)
    {
        string? normalizedActual = TryNormalizeJson(actual);
        string? normalizedExpected = TryNormalizeJson(expected);

        return normalizedActual is not null && normalizedExpected is not null
            ? normalizedActual == normalizedExpected
            : string.Equals(actual.Trim(), expected.Trim(), StringComparison.Ordinal);
    }

    public static string? TryNormalizeJson(string value)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(value.Trim());
            return JsonSerializer.Serialize(doc);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? ParseActualOutput(string? stdout)
    {
        if (string.IsNullOrEmpty(stdout))
            return null;

        string[] lines = stdout.TrimEnd('\r', '\n').Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        string actual = lines[^1];
        return string.IsNullOrWhiteSpace(actual) ? null : actual.Trim();
    }
}