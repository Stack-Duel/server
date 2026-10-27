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

public sealed record StepHandlerResult(
    bool Succeeded,
    string? Error = null,
    string? RequestPayload = null,
    string? ResponsePayload = null,
    bool IsRetryable = true
);

public interface IStepHandler
{
    bool CanHandle(Domain.ExecutionPipelines.Enums.ExecutionPipelineStepType stepType);
    Task<StepHandlerResult> ExecuteAsync(StepHandlerContext context);
}