using StackDuel.Application.ExecutionEngine;
using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.Languages.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace StackDuel.Infrastructure.ExecutionEngine.StepHandlers;

/// <summary>
/// Polls Judge0 for the tokens stored by the execute step.
/// Returns <see cref="StepHandlerResult.Succeeded"/> = true only when all tokens have left
/// the queued/processing/internal-error state. The processor should retry (up to MaxAttempts)
/// if still pending. Stores the polled results JSON in <see cref="StepHandlerResult.ResponsePayload"/>.
///
/// Branches on the execute step's recorded <see cref="TestExecutionMode"/>: <c>PerCase</c>
/// polls one token per test case exactly as before; <c>BatchedTestFramework</c> polls the
/// single batch token and expands its one result into one enriched row per test case via the
/// language's <see cref="IBatchCodeTemplateStrategy.ParseBatchOutput"/> — each such row already
/// carries its final verdict (Vitest's own assertion decided it), so it's marked
/// <c>Precomputed</c> for <c>EvaluateStepHandler</c> to trust as-is.
/// </summary>
internal sealed partial class Judge0PollStepHandler(
    IExecutionEngineStrategy engine,
    ICodeTemplateStrategyResolver templateResolver,
    IBatchCodeTemplateStrategyResolver batchTemplateResolver,
    ILogger<Judge0PollStepHandler> logger
) : IStepHandler
{
    public bool CanHandle(ExecutionPipelineStepType stepType) => stepType == ExecutionPipelineStepType.Judge0Poll;

    public async Task<StepHandlerResult> ExecuteAsync(StepHandlerContext context)
    {
        var job = context.Job;
        var attempt = context.Attempt;
        var ct = context.CancellationToken;

        string? previousResponse = job
            .Attempts.Where(a => a.PipelineStepId != attempt.PipelineStepId)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => a.ResponsePayload)
            .FirstOrDefault(p => p is not null);

        if (previousResponse is null)
            return new StepHandlerResult(Succeeded: false, Error: "No token map found from execute step.");

        Judge0ExecutionStepHandler.ExecutePayload? executePayload;
        try
        {
            executePayload = JsonSerializer.Deserialize<Judge0ExecutionStepHandler.ExecutePayload>(previousResponse);
        }
        catch
        {
            return new StepHandlerResult(Succeeded: false, Error: "Could not deserialize token map from execute step.");
        }

        if (executePayload is null)
            return new StepHandlerResult(Succeeded: false, Error: "Token map is empty.");

        return executePayload.Mode == TestExecutionMode.BatchedTestFramework
            ? await PollBatchedAsync(executePayload, job.SubmissionId, ct)
            : await PollPerCaseAsync(executePayload, job.SubmissionId, ct);
    }

    private async Task<StepHandlerResult> PollPerCaseAsync(
        Judge0ExecutionStepHandler.ExecutePayload executePayload,
        Guid submissionId,
        CancellationToken ct
    )
    {
        if (executePayload.Tokens.Count == 0)
            return new StepHandlerResult(Succeeded: false, Error: "Token map is empty.");

        Dictionary<string, Guid> tokenMap = executePayload.Tokens;
        ICodeTemplateStrategy templateStrategy = templateResolver.Resolve(executePayload.LanguageName);

        LogPolling(tokenMap.Count, submissionId);

        IReadOnlyList<ExecutionEngineResult> results;
        try
        {
            results = await engine.PollBatchAsync(tokenMap.Keys.ToList(), ct);
        }
        catch (Exception ex)
        {
            LogPollFailed(submissionId, ex);
            return new StepHandlerResult(Succeeded: false, Error: $"Judge0 poll failed: {ex.Message}");
        }

        if (!AllDone(results, submissionId))
            return new StepHandlerResult(Succeeded: false, Error: "Submissions still processing; will retry.");

        var enriched = results.Select(r =>
        {
            var parsedOutput = templateStrategy.ParseOutput(r.Stdout);

            return new PollResultEntry(
                r.Token,
                tokenMap.TryGetValue(r.Token, out var id) ? id : Guid.Empty,
                parsedOutput.UserLogs,
                parsedOutput.ActualResult,
                r.Stderr,
                r.CompileOutput,
                r.RuntimeMs,
                r.MemoryUsedKb,
                r.Status.ToString(),
                Precomputed: false
            );
        });

        return new StepHandlerResult(Succeeded: true, ResponsePayload: JsonSerializer.Serialize(enriched));
    }

    private async Task<StepHandlerResult> PollBatchedAsync(
        Judge0ExecutionStepHandler.ExecutePayload executePayload,
        Guid submissionId,
        CancellationToken ct
    )
    {
        if (executePayload.BatchToken is null || executePayload.BatchTestCaseIds is null)
            return new StepHandlerResult(Succeeded: false, Error: "Batch token or case list is missing.");

        LogPolling(1, submissionId);

        IReadOnlyList<ExecutionEngineResult> results;
        try
        {
            results = await engine.PollBatchAsync([executePayload.BatchToken], ct);
        }
        catch (Exception ex)
        {
            LogPollFailed(submissionId, ex);
            return new StepHandlerResult(Succeeded: false, Error: $"Judge0 poll failed: {ex.Message}");
        }

        if (!AllDone(results, submissionId))
            return new StepHandlerResult(Succeeded: false, Error: "Submission still processing; will retry.");

        ExecutionEngineResult result = results[0];

        // The Judge0 submission itself (one Node process running every case) failed to even
        // produce a Vitest report — e.g. it crashed or was killed before finishing. Every case
        // it was supposed to cover is reported as a runtime error rather than silently dropped.
        if (result.Status != ExecutionEngineResultStatus.Accepted)
        {
            var failedRows = executePayload.BatchTestCaseIds.Select(id => new PollResultEntry(
                executePayload.BatchToken,
                id,
                null,
                null,
                result.Stderr,
                result.CompileOutput,
                result.RuntimeMs,
                result.MemoryUsedKb,
                result.Status.ToString(),
                Precomputed: true
            ));

            return new StepHandlerResult(Succeeded: true, ResponsePayload: JsonSerializer.Serialize(failedRows));
        }

        IBatchCodeTemplateStrategy batchStrategy = batchTemplateResolver.Resolve(executePayload.LanguageName);
        IReadOnlyList<BatchCaseResult> batchResults = batchStrategy.ParseBatchOutput(
            result.Stdout,
            executePayload.BatchTestCaseIds
        );

        var enriched = batchResults.Select(br => new PollResultEntry(
            executePayload.BatchToken,
            br.TestCaseId,
            null,
            br.ActualOutput,
            result.Stderr,
            result.CompileOutput,
            br.RuntimeMs ?? result.RuntimeMs,
            result.MemoryUsedKb,
            MapBatchStatus(br.Status),
            Precomputed: true
        ));

        return new StepHandlerResult(Succeeded: true, ResponsePayload: JsonSerializer.Serialize(enriched));
    }

    // InternalError is Judge0's own "something went wrong in the sandbox" status (e.g. a
    // transient RapidAPI/cgroup hiccup) — treated the same as still-pending rather than
    // terminal, since it's frequently transient and there's no SubmissionResultStatus for it
    // anyway. Bounded by the step's existing MaxAttempts, same as Queued/Processing.
    private bool AllDone(IReadOnlyList<ExecutionEngineResult> results, Guid submissionId)
    {
        bool allDone = results.All(r =>
            r.Status != ExecutionEngineResultStatus.Queued
            && r.Status != ExecutionEngineResultStatus.Processing
            && r.Status != ExecutionEngineResultStatus.InternalError
        );

        if (!allDone)
        {
            LogStillPending(
                results.Count(r =>
                    r.Status
                        is ExecutionEngineResultStatus.Queued
                            or ExecutionEngineResultStatus.Processing
                            or ExecutionEngineResultStatus.InternalError
                ),
                submissionId
            );
        }

        return allDone;
    }

    private static string MapBatchStatus(BatchCaseStatus status) =>
        status switch
        {
            BatchCaseStatus.Accepted => "Accepted",
            BatchCaseStatus.WrongAnswer => "WrongAnswer",
            BatchCaseStatus.TimedOut => "TimeLimitExceeded",
            _ => "RuntimeError",
        };

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Polling {Count} Judge0 tokens for submission {SubmissionId}"
    )]
    private partial void LogPolling(int count, Guid submissionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Count} tokens still pending for submission {SubmissionId}")]
    private partial void LogStillPending(int count, Guid submissionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Judge0 poll failed for submission {SubmissionId}")]
    private partial void LogPollFailed(Guid submissionId, Exception exception);

    /// <summary>
    /// <paramref name="Precomputed"/> marks a row whose <paramref name="Status"/> is already a
    /// final verdict (set by the batched path, where Vitest's own assertion decided
    /// pass/fail) rather than a raw Judge0 engine status still needing comparison against the
    /// expected output in <c>EvaluateStepHandler</c>.
    /// </summary>
    internal sealed record PollResultEntry(
        string Token,
        Guid TestCaseId,
        string? Stdout,
        string? ActualOutput,
        string? Stderr,
        string? CompileOutput,
        int? RuntimeMs,
        int? MemoryUsedKb,
        string? Status,
        bool Precomputed
    );
}