using StackDuel.Application.ExecutionEngine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

/// <summary>
/// Renders every test case for one submission into a single Vitest spec and parses Vitest's
/// own Jest-compatible JSON reporter back into a per-case verdict. Pairs with a
/// <c>LanguageVersionEntry</c> whose <c>ExecutionMode</c> is
/// <see cref="StackDuel.Domain.Languages.Enums.TestExecutionMode.BatchedTestFramework"/> and
/// whose Judge0 image actually has Vitest installed — this is a distinct language version
/// from plain "JavaScript", not a replacement for it.
///
/// The Judge0 run command for that image must: run Vitest with its JSON report sent to a file
/// (<c>--reporter=json --outputFile=&lt;path&gt;</c>), not stdout — Vitest's own console/progress
/// output must not land on stdout, only this harness's own <c>console.log</c> calls — then
/// print <see cref="VitestReportMarker"/> and cat that file to stdout. Concretely:
/// <c>vitest run --reporter=json --outputFile=report.json &amp;&amp; echo "===VITEST_REPORT===" &amp;&amp; cat report.json</c>.
/// Real stdout therefore looks like
/// <c>===ACTUAL_OUTPUTS===&lt;outputs json&gt;===VITEST_REPORT===&lt;vitest report json&gt;</c> —
/// the outputs block (printed by this harness's own <c>afterAll</c>, which runs before the
/// Vitest process exits) comes first, the report (cat'd only after that process exits) second.
/// </summary>
internal sealed class VitestJavaScriptBatchTemplateStrategy : IBatchCodeTemplateStrategy
{
    private const string ActualOutputsMarker = "===ACTUAL_OUTPUTS===";
    private const string VitestReportMarker = "===VITEST_REPORT===";

    public string LanguageName => "JavaScript";

    public string RenderBatch(
        string userCode,
        string? functionName,
        IReadOnlyList<BatchTestCase> cases,
        int perCaseTimeoutMs,
        IReadOnlyList<CodeTemplateSourceFile>? additionalFiles = null
    )
    {
        string casesJson = JsonSerializer.Serialize(
            cases.Select(c => new HarnessCase(
                c.TestCaseId.ToString(),
                c.Inputs.Select(ToJsonElement).ToArray(),
                c.ExpectedOutput,
                c.AssertStrategy.ToString(),
                c.Tolerance,
                c.CaseSensitive
            ))
        );

        return $$"""
            import { describe, it, expect, afterAll } from "vitest";

            {{userCode}}

            const __cases = {{casesJson}};
            const __outputs = {};

            // Mirrors EvaluateStepHandler.NormalizeJson's parse-or-fallback behavior: most
            // expected/actual values are JSON-shaped ("[0,1]", "true"), but a plain string
            // result (e.g. "reversed") is not valid JSON on its own — JSON.parse would throw
            // and crash the whole case instead of comparing it. Falling back to the raw string
            // keeps non-JSON values comparable instead of a parse failure masquerading as WrongAnswer.
            function __normalize(value) {
                try {
                    return JSON.parse(value);
                } catch {
                    return value;
                }
            }

            describe("generated", () => {
                for (const c of __cases) {
                    it(c.id, { timeout: {{perCaseTimeoutMs}} }, () => {
                        const result = {{functionName}}(...c.args);
                        const actual = typeof result === "string" ? result : JSON.stringify(result);
                        __outputs[c.id] = actual;

                        switch (c.strategy) {
                            case "FloatTolerance": {
                                const diff = Math.abs(Number(actual) - Number(c.expected));
                                expect(diff).toBeLessThanOrEqual(c.tolerance ?? 1e-6);
                                break;
                            }
                            case "SetEquality":
                                expect(__normalize(actual)).toEqual(__normalize(c.expected));
                                break;
                            default:
                                if (c.caseSensitive) {
                                    expect(actual.trim()).toBe(c.expected.trim());
                                } else {
                                    expect(actual.trim().toLowerCase()).toBe(c.expected.trim().toLowerCase());
                                }
                        }
                    });
                }

                afterAll(() => {
                    console.log("{{ActualOutputsMarker}}");
                    console.log(JSON.stringify(__outputs));
                });
            });
            """;
    }

    public IReadOnlyList<BatchCaseResult> ParseBatchOutput(string? stdout, IReadOnlyList<Guid> orderedTestCaseIds)
    {
        if (string.IsNullOrWhiteSpace(stdout))
            return [.. orderedTestCaseIds.Select(id => new BatchCaseResult(id, BatchCaseStatus.RuntimeError, null, "No output from Vitest."))];

        (string reportJson, Dictionary<string, string>? actualOutputs) = SplitStdout(stdout);

        VitestReport? report;
        try
        {
            report = JsonSerializer.Deserialize<VitestReport>(reportJson);
        }
        catch (JsonException)
        {
            return [.. orderedTestCaseIds.Select(id => new BatchCaseResult(id, BatchCaseStatus.RuntimeError, null, "Could not parse Vitest report."))];
        }

        Dictionary<string, VitestAssertionResult> byTitle = (report?.TestResults ?? [])
            .SelectMany(r => r.AssertionResults)
            .GroupBy(a => a.Title)
            .ToDictionary(g => g.Key, g => g.First());

        return
        [
            .. orderedTestCaseIds.Select(id =>
            {
                string caseId = id.ToString();
                string? actual = actualOutputs is not null && actualOutputs.TryGetValue(caseId, out string? o) ? o : null;

                if (!byTitle.TryGetValue(caseId, out VitestAssertionResult? assertion))
                    return new BatchCaseResult(id, BatchCaseStatus.RuntimeError, actual, "No matching Vitest result for this case.");

                string? failureMessage = assertion.FailureMessages.FirstOrDefault();

                BatchCaseStatus status = assertion.Status switch
                {
                    "passed" => BatchCaseStatus.Accepted,
                    "failed" when IsTimeout(failureMessage) => BatchCaseStatus.TimedOut,
                    "failed" when IsAssertionFailure(failureMessage) => BatchCaseStatus.WrongAnswer,
                    "failed" => BatchCaseStatus.RuntimeError,
                    _ => BatchCaseStatus.RuntimeError,
                };

                return new BatchCaseResult(
                    id,
                    status,
                    actual,
                    status == BatchCaseStatus.Accepted ? null : failureMessage,
                    assertion.Duration
                );
            }),
        ];
    }

    private static (string ReportJson, Dictionary<string, string>? ActualOutputs) SplitStdout(string stdout)
    {
        int outputsIndex = stdout.IndexOf(ActualOutputsMarker, StringComparison.Ordinal);
        int reportIndex = stdout.IndexOf(VitestReportMarker, StringComparison.Ordinal);

        // Markers missing or out of the expected order (outputs block, then report) means the
        // run command didn't produce what this strategy expects — fall back to treating the
        // whole thing as the report so the caller still gets a "could not parse" result instead
        // of silently misreading unrelated bytes as one half of the split.
        if (outputsIndex < 0 || reportIndex < 0 || reportIndex < outputsIndex)
            return (stdout, null);

        string outputsJson = stdout[(outputsIndex + ActualOutputsMarker.Length)..reportIndex].Trim();
        string reportJson = stdout[(reportIndex + VitestReportMarker.Length)..].Trim();

        try
        {
            return (reportJson, JsonSerializer.Deserialize<Dictionary<string, string>>(outputsJson));
        }
        catch (JsonException)
        {
            return (reportJson, null);
        }
    }

    private static bool IsTimeout(string? message) =>
        message is not null && message.Contains("timed out", StringComparison.OrdinalIgnoreCase);

    private static bool IsAssertionFailure(string? message) =>
        message is not null
        && (message.Contains("AssertionError", StringComparison.Ordinal) || message.Contains("expected", StringComparison.OrdinalIgnoreCase));

    private static JsonElement ToJsonElement(CodeTemplateInput input)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(input.Value);
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(input.Value);
        }
    }

    private sealed record HarnessCase(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("args")] JsonElement[] Args,
        [property: JsonPropertyName("expected")] string Expected,
        [property: JsonPropertyName("strategy")] string Strategy,
        [property: JsonPropertyName("tolerance")] decimal? Tolerance,
        [property: JsonPropertyName("caseSensitive")] bool CaseSensitive
    );

    private sealed record VitestReport([property: JsonPropertyName("testResults")] List<VitestTestFileResult> TestResults);

    private sealed record VitestTestFileResult(
        [property: JsonPropertyName("assertionResults")] List<VitestAssertionResult> AssertionResults
    );

    private sealed record VitestAssertionResult(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("failureMessages")] List<string> FailureMessages,
        [property: JsonPropertyName("duration")] int? Duration
    );
}