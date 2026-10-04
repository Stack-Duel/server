using StackDuel.Application.ExecutionEngine;
using StackDuel.Domain.TestSuites.Entities;
using System.Text.Json;

namespace StackDuel.Infrastructure.ExecutionEngine;

/// <summary>
/// Runs a reference solution against a set of test cases via Judge0 and reports pass/fail.
/// Shared by <see cref="TestCaseGeneration.TestCaseGenerationService"/> (sanity-checking a
/// reference solution before trusting it to generate ground truth) and
/// <see cref="ProblemValidation.ReferenceSolutionValidationService"/> (validating an admin-authored
/// reference solution against admin-authored sample cases before a problem can publish).
/// </summary>
internal sealed class Judge0ReferenceSolutionRunner(IExecutionEngineStrategy executionEngine)
{
    private const int MaxPollAttempts = 30;
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(2);

    /// <summary>Runs the reference solution against each case and reports one warning per case that fails.</summary>
    public async Task<List<string>> RunAgainstCasesAsync(
        string referenceSolutionCode,
        string? functionName,
        ICodeTemplateStrategy templateStrategy,
        int judge0LanguageId,
        IReadOnlyList<TestCase> testCases,
        CancellationToken cancellationToken
    )
    {
        List<List<CodeTemplateInput>> inputsByCase =
        [
            .. testCases.Select(tc => tc.Inputs.Select(i => new CodeTemplateInput(i.Value, i.ValueType)).ToList()),
        ];

        List<ExecutionEngineSubmission> submissions =
        [
            .. inputsByCase.Select(inputs => new ExecutionEngineSubmission(
                SourceCode: templateStrategy.Render(
                    new CodeTemplateContext(referenceSolutionCode, functionName, inputs)
                ),
                LanguageId: judge0LanguageId,
                Stdin: templateStrategy.BuildStdin(inputs),
                TimeLimitMs: null,
                MemoryLimitKb: null
            )),
        ];

        List<ExecutionEngineResult> results = await SubmitAndPollAsync(submissions, cancellationToken);

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

    /// <summary>
    /// Compares two output values the same tolerant way <c>AssertStrategy.SetEquality</c>
    /// does in <c>EvaluateStepHandler</c>: array/object outputs are parsed as JSON and
    /// re-serialized before comparing, so cosmetic differences (e.g. Python's
    /// <c>print([1, 2])</c> vs. the stored <c>"[1,2]"</c>) don't make a correct reference
    /// solution look broken. Falls back to a trimmed string comparison for anything that
    /// isn't valid JSON.
    /// </summary>
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

        // A single trailing newline (the normal case for print()/console.log()) would
        // otherwise show up as a spurious empty final "line" — mirrors Judge0PollStepHandler.
        string[] lines = stdout.TrimEnd('\r', '\n').Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        string actual = lines[^1];
        return string.IsNullOrWhiteSpace(actual) ? null : actual.Trim();
    }
}