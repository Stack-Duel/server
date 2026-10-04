using StackDuel.Domain.ExecutionPipelines.Entities;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.SubmissionJobs.Entities;

namespace StackDuel.Application.ExecutionEngine;

public sealed record StepHandlerContext(
    SubmissionJob Job,
    ExecutionPipelineStep Step,
    SubmissionJobAttempt Attempt,
    CancellationToken CancellationToken
);

/// <param name="Succeeded">Whether the step completed successfully.</param>
/// <param name="Error">Error message when <see cref="Succeeded"/> is <c>false</c>.</param>
/// <param name="RequestPayload">Optional JSON payload to persist as the attempt request.</param>
/// <param name="ResponsePayload">Optional JSON payload to persist as the attempt response.</param>
public sealed record StepHandlerResult(
    bool Succeeded,
    string? Error = null,
    string? RequestPayload = null,
    string? ResponsePayload = null
);

public interface IStepHandler
{
    bool CanHandle(Domain.ExecutionPipelines.Enums.ExecutionPipelineStepType stepType);
    Task<StepHandlerResult> ExecuteAsync(StepHandlerContext context);
}