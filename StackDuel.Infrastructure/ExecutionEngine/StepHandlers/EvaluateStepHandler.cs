using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Events;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Exceptions;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Infrastructure.ExecutionEngine.Assert;
using StackDuel.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.Infrastructure.ExecutionEngine.StepHandlers;

internal sealed partial class EvaluateStepHandler(
    StackDuelDbContext db,
    IDomainEventDispatcher domainEventDispatcher,
    ILogger<EvaluateStepHandler> logger
) : IStepHandler
{
    // JsonSerializer.Serialize with no options writes enums as their numeric value —
    // unlike the API's response formatter (configured with JsonStringEnumConverter in
    // ApiServiceRegistration), this call has no such config, so without an explicit
    // converter here the stored response payload would be the unreadable "{"Status":2}"
    // instead of "{"Status":"Accepted"}" in the admin diagnostics view.
    private static readonly JsonSerializerOptions ResponsePayloadOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public bool CanHandle(ExecutionPipelineStepType stepType) => stepType == ExecutionPipelineStepType.Evaluate;

    public async Task<StepHandlerResult> ExecuteAsync(StepHandlerContext context)
    {
        var job = context.Job;
        var ct = context.CancellationToken;

        var (pollResults, pollError) = LoadPollResults(job, context.Attempt.PipelineStepId);
        if (pollError is not null)
            return Fail(pollError);

        var submission = await db
            .Submissions.Include(s => s.Results)
            .FirstOrDefaultAsync(s => s.Id == job.SubmissionId, ct);

        if (submission is null)
            return Fail($"Submission {job.SubmissionId} not found.");

        var configuredAssert = await db.AssertStepConfigurations.FirstOrDefaultAsync(
            c => c.PipelineStepId == context.Step.Id,
            ct
        );

        var testCaseIds = pollResults!.Select(r => r.TestCaseId).ToList();
        var testCases = await db.Set<TestCase>()
            .Include(tc => tc.ExpectedOutputs)
            .Where(tc => testCaseIds.Contains(tc.Id))
            .ToDictionaryAsync(tc => tc.Id, ct);

        ApplyPollResults(pollResults!, testCases, configuredAssert, submission);

        try
        {
            submission.Complete();
        }
        catch (SubmissionNotCompleteException ex)
        {
            return Fail($"Could not complete submission: {ex.Message}");
        }

        await db.SaveChangesAsync(ct);

        await domainEventDispatcher.DispatchAsync(submission.PopDomainEvents(), ct);

        LogEvaluated(job.SubmissionId, submission.Status.ToString());

        return new StepHandlerResult(
            Succeeded: true,
            ResponsePayload: JsonSerializer.Serialize(new { submission.Status }, ResponsePayloadOptions)
        );
    }

    private static (List<PollResultEntry>? Results, string? Error) LoadPollResults(
        SubmissionJob job,
        Guid currentPipelineStepId
    )
    {
        string? pollResponse = job
            .Attempts.Where(a => a.PipelineStepId != currentPipelineStepId)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => a.ResponsePayload)
            .FirstOrDefault(p => p is not null);

        if (pollResponse is null)
            return (null, "No poll response found from poll step.");

        List<PollResultEntry>? pollResults;
        try
        {
            pollResults = JsonSerializer.Deserialize<List<PollResultEntry>>(
                pollResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch
        {
            return (null, "Could not deserialize poll results.");
        }

        if (pollResults is null || pollResults.Count == 0)
            return (null, "Poll results are empty.");

        return (pollResults, null);
    }

    private static void ApplyPollResults(
        List<PollResultEntry> pollResults,
        Dictionary<Guid, TestCase> testCases,
        AssertStepConfiguration? configuredAssert,
        Submission submission
    )
    {
        foreach (var pollResult in pollResults)
        {
            if (pollResult.TestCaseId == Guid.Empty)
                continue;

            SubmissionResultStatus status = MapEngineStatus(pollResult.Status);

            if (
                status == SubmissionResultStatus.Accepted
                && !pollResult.Precomputed
                && testCases.TryGetValue(pollResult.TestCaseId, out TestCase? testCase)
            )
            {
                TestCaseExpectedOutput? expectedOutput = testCase.ExpectedOutputs.OrderBy(e => e.Id).FirstOrDefault();
                string? expected = expectedOutput?.Value;

                if (submission.Type != SubmissionType.Run || expected is not null)
                {
                    string? actual = pollResult.ActualOutput?.TrimEnd('\n', '\r', ' ');
                    AssertStepConfiguration assertConfig =
                        configuredAssert ?? DefaultAssertConfigFor(expectedOutput?.ValueType);
                    status = Evaluate(actual, expected, assertConfig);
                }
            }

            submission.UpdateResult(
                testCaseId: pollResult.TestCaseId,
                status: status,
                runtime: pollResult.RuntimeMs,
                memoryUsed: pollResult.MemoryUsedKb,
                actualOutput: pollResult.ActualOutput,
                standardOutput: pollResult.Stdout,
                standardError: pollResult.Stderr,
                compileOutput: pollResult.CompileOutput
            );
        }
    }

    private static AssertStepConfiguration DefaultAssertConfigFor(string? valueType) =>
        valueType is "double" or "float"
            ? new AssertStepConfiguration { Strategy = AssertStrategy.FloatTolerance, Tolerance = 1e-6m }
            : new AssertStepConfiguration { Strategy = AssertStrategy.SetEquality, CaseSensitive = true };

    private static SubmissionResultStatus Evaluate(string? actual, string? expected, AssertStepConfiguration config)
    {
        if (actual is null || expected is null)
            return SubmissionResultStatus.WrongAnswer;

        bool match = config.Strategy switch
        {
            AssertStrategy.FloatTolerance => TryFloatCompare(actual, expected, config.Tolerance ?? 1e-6m),
            AssertStrategy.SetEquality => NormalizeJson(actual) == NormalizeJson(expected),
            _ => config.CaseSensitive
                ? actual.Trim() == expected.Trim()
                : string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase),
        };

        return match ? SubmissionResultStatus.Accepted : SubmissionResultStatus.WrongAnswer;
    }

    private static bool TryFloatCompare(string actual, string expected, decimal tolerance)
    {
        if (decimal.TryParse(actual.Trim(), out decimal a) && decimal.TryParse(expected.Trim(), out decimal e))
            return Math.Abs(a - e) <= tolerance;
        return false;
    }

    private static string NormalizeJson(string value)
    {
        try
        {
            var doc = JsonDocument.Parse(value.Trim());
            return JsonSerializer.Serialize(doc);
        }
        catch
        {
            return value.Trim();
        }
    }

    private static SubmissionResultStatus MapEngineStatus(string? status) =>
        status switch
        {
            "Accepted" => SubmissionResultStatus.Accepted,
            "WrongAnswer" => SubmissionResultStatus.WrongAnswer,
            "TimeLimitExceeded" => SubmissionResultStatus.TimeLimitExceeded,
            "CompilationError" => SubmissionResultStatus.CompileError,
            "RuntimeError" => SubmissionResultStatus.RuntimeError,
            _ => SubmissionResultStatus.RuntimeError,
        };

    private static StepHandlerResult Fail(string error) => new(Succeeded: false, Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Evaluated submission {SubmissionId}: {Status}")]
    private partial void LogEvaluated(Guid submissionId, string status);

    /// <summary>
    /// <paramref name="Precomputed"/> is set by the batched execution path (see
    /// <c>Judge0PollStepHandler</c>) — <paramref name="Status"/> there is already the final
    /// verdict decided by the language's own test framework, so it must not be re-derived here
    /// via <see cref="Evaluate"/> against the stored expected output.
    /// </summary>
    private sealed record PollResultEntry(
        Guid TestCaseId,
        string? Stdout,
        string? ActualOutput,
        string? Stderr,
        string? CompileOutput,
        int? RuntimeMs,
        int? MemoryUsedKb,
        string? Status,
        string? Token,
        bool Precomputed = false
    );
}