using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Events;
using StackDuel.Application.ExecutionEngine;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Domain.Submissions;

namespace StackDuel.Infrastructure.Jobs.Submissions;

/// <summary>
/// Picks up pending <see cref="SubmissionJob"/> entries and processes them.
/// Creates a new DI scope per job so each job gets its own isolated DbContext.
/// </summary>
internal sealed partial class SubmissionJobProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<SubmissionJobProcessorService> logger
)
{
    private const int BatchSize = 20;

    // How long a worker holds exclusive claim on a job while processing a single
    // step. Must comfortably exceed the slowest step's execution time; if a worker
    // crashes mid-step the lease simply expires and another worker can pick it up.
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<SubmissionJob> jobs;
        using (var scope = scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ISubmissionJobRepository>();
            jobs = await repo.FindPendingAsync(BatchSize, cancellationToken);
        }

        LogFound(jobs.Count);

        foreach (var job in jobs)
        {
            await ProcessInNewScopeAsync(job.Id, cancellationToken);
        }
    }

    public async Task RunForSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        Guid? jobId;
        using (var scope = scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ISubmissionJobRepository>();
            var job = await repo.FindBySubmissionIdAsync(submissionId, cancellationToken);
            if (job is null)
            {
                LogSubmissionJobNotFound(submissionId);
                return;
            }

            if (job.Status is SubmissionJobStatus.Completed or SubmissionJobStatus.Failed)
            {
                LogJobAlreadyProcessed(job.Id);
                return;
            }

            jobId = job.Id;
        }

        await ProcessInNewScopeAsync(jobId.Value, cancellationToken);
    }

    /// <summary>
    /// Loads and processes a single job inside its own DI scope and DbContext.
    /// </summary>
    private async Task ProcessInNewScopeAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var jobRepository = sp.GetRequiredService<ISubmissionJobRepository>();

        // Atomically claim the job so the Quartz sweep and the RabbitMQ-driven
        // continuation flow can never process the same job at the same time.
        bool claimed = await jobRepository.TryClaimAsync(jobId, LeaseDuration, ct);
        if (!claimed)
        {
            LogJobAlreadyLocked(jobId);
            return;
        }

        try
        {
            var pipelineRepository = sp.GetRequiredService<IExecutionPipelineRepository>();
            var handlerRegistry = sp.GetRequiredService<IStepHandlerRegistry>();
            var messagePublisher = sp.GetRequiredService<IMessagePublisher>();
            var submissionRepository = sp.GetRequiredService<ISubmissionWriteRepository>();
            var domainEventDispatcher = sp.GetRequiredService<IDomainEventDispatcher>();

            var job = await jobRepository.FindByIdAsync(jobId, ct);
            if (job is null)
            {
                logger.LogWarning("Job {JobId} not found when processing in isolated scope.", jobId);
                return;
            }

            if (job.Status is SubmissionJobStatus.Completed or SubmissionJobStatus.Failed)
            {
                LogJobAlreadyProcessed(job.Id);
                return;
            }

            await ProcessJobAsync(
                job,
                jobRepository,
                pipelineRepository,
                handlerRegistry,
                messagePublisher,
                submissionRepository,
                domainEventDispatcher,
                ct
            );
        }
        finally
        {
            await jobRepository.ReleaseLockAsync(jobId, CancellationToken.None);
        }
    }

    private async Task ProcessJobAsync(
        SubmissionJob job,
        ISubmissionJobRepository jobRepository,
        IExecutionPipelineRepository pipelineRepository,
        IStepHandlerRegistry handlerRegistry,
        IMessagePublisher messagePublisher,
        ISubmissionWriteRepository submissionRepository,
        IDomainEventDispatcher domainEventDispatcher,
        CancellationToken ct
    )
    {
        if (job.CurrentStepId is null)
        {
            job.Complete();
            await jobRepository.UpdateAsync(job, ct);
            return;
        }

        var pipeline = await pipelineRepository.FindByIdWithStepsAsync(job.PipelineId, ct);
        if (pipeline is null)
        {
            job.Fail($"Pipeline {job.PipelineId} not found.");
            await jobRepository.UpdateAsync(job, ct);
            await FailSubmissionAsync(job.SubmissionId, submissionRepository, domainEventDispatcher, ct);
            return;
        }

        var step = pipeline.Steps.FirstOrDefault(s => s.Id == job.CurrentStepId);
        if (step is null)
        {
            job.Fail($"Step {job.CurrentStepId} not found in pipeline {job.PipelineId}.");
            await jobRepository.UpdateAsync(job, ct);
            await FailSubmissionAsync(job.SubmissionId, submissionRepository, domainEventDispatcher, ct);
            return;
        }

        int priorAttempts = job.AttemptCountForCurrentStep();
        if (priorAttempts >= step.MaxAttempts)
        {
            job.Fail($"Step '{step.Name}' exceeded max attempts ({step.MaxAttempts}).");
            await jobRepository.UpdateAsync(job, ct);
            await FailSubmissionAsync(job.SubmissionId, submissionRepository, domainEventDispatcher, ct);
            return;
        }

        var attempt = job.StartAttempt();

        await jobRepository.PersistAttemptAsync(job, attempt, ct);

        IStepHandler handler;
        try
        {
            handler = handlerRegistry.Resolve(step.StepType);
        }
        catch (InvalidOperationException ex)
        {
            attempt.Fail(ex.Message);
            job.Fail(ex.Message);
            await jobRepository.UpdateAsync(job, ct);
            await FailSubmissionAsync(job.SubmissionId, submissionRepository, domainEventDispatcher, ct);
            return;
        }

        LogExecutingStep(job.Id, step.Name, step.StepType.ToString());

        StepHandlerResult result;
        try
        {
            result = await handler.ExecuteAsync(new StepHandlerContext(job, step, attempt, ct));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception in step handler '{StepType}' for job {JobId}.",
                step.StepType,
                job.Id
            );
            result = new StepHandlerResult(Succeeded: false, Error: $"Unhandled exception: {ex.Message}");
        }

        if (result.Succeeded)
        {
            attempt.Succeed(result.RequestPayload, result.ResponsePayload);

            var nextStep = pipeline.NextStep(step.StepOrder);
            if (nextStep is not null)
            {
                job.AdvanceTo(nextStep.Id);
                LogAdvanced(job.Id, nextStep.Name);
            }
            else
            {
                job.Complete();
                LogCompleted(job.Id);
            }
        }
        else
        {
            attempt.Fail(result.Error ?? "Unknown error.", result.RequestPayload, result.ResponsePayload);

            bool hasRetriesLeft = job.AttemptCountForCurrentStep() < step.MaxAttempts;
            if (!hasRetriesLeft)
            {
                job.Fail($"Step '{step.Name}' failed after {step.MaxAttempts} attempt(s): {result.Error}");
                LogStepFailed(job.Id, step.Name, result.Error);
                await jobRepository.UpdateAsync(job, ct);
                await FailSubmissionAsync(job.SubmissionId, submissionRepository, domainEventDispatcher, ct);
                return;
            }
            else
            {
                job.ResetToPending();
                LogStepRetrying(job.Id, step.Name, job.AttemptCountForCurrentStep(), step.MaxAttempts);
            }
        }

        await jobRepository.UpdateAsync(job, ct);

        // Always release the lock BEFORE publishing any continuation message.
        // If we publish while still holding the lock, the second consumer
        // (ConsumerConcurrency=2) can immediately receive the message, call
        // TryClaimAsync, see the unexpired lease, log "already locked", ack the
        // message, and discard it — leaving the job stranded until the Quartz
        // sweep fires (~2 min). Releasing first guarantees the next consumer
        // can always claim the job.
        if (job.Status == SubmissionJobStatus.Pending)
        {
            await jobRepository.ReleaseLockAsync(job.Id, CancellationToken.None);

            bool isRetryOfSameStep = job.CurrentStepId == step.Id;
            if (isRetryOfSameStep && step.IsPolling)
            {
                var delay = ComputePollDelay(job.AttemptCountForCurrentStep(), step.TimeoutSeconds);
                await Task.Delay(delay, ct);
            }

            await messagePublisher.PublishAsync(new SubmissionJobContinuationMessage(job.SubmissionId), ct);
        }
    }

    /// <summary>
    /// Marks all non-terminal submission results as <see cref="SubmissionResultStatus.RuntimeError"/>
    /// and transitions the submission to a terminal state so the client stops polling.
    /// </summary>
    private async Task FailSubmissionAsync(
        Guid submissionId,
        ISubmissionWriteRepository submissionRepository,
        IDomainEventDispatcher domainEventDispatcher,
        CancellationToken ct
    )
    {
        try
        {
            var submission = await submissionRepository.FindByIdAsync(submissionId, ct);
            if (
                submission is null
                || submission.Status
                    is Domain.Submissions.Enums.SubmissionStatus.Accepted
                        or Domain.Submissions.Enums.SubmissionStatus.WrongAnswer
            )
                return;

            submission.Fail();
            await submissionRepository.UpdateAsync(submission, ct);
            await domainEventDispatcher.DispatchAsync(submission.PopDomainEvents(), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark submission {SubmissionId} as failed after job failure.", submissionId);
        }
    }

    // Most Judge0 submissions finish within a couple hundred milliseconds once
    // dequeued (confirmed: avg poll attempt duration is ~213ms, and most jobs
    // only need 3 poll attempts). A flat 1s delay between attempts wastes time
    // on that common fast case. Start fast and back off exponentially instead,
    // capped at the step's configured TimeoutSeconds so the worst-case total
    // wait budget (MaxAttempts * cap) is unchanged.
    private const int PollBaseDelayMs = 200;
    private const double PollBackoffMultiplier = 1.7;

    private static TimeSpan ComputePollDelay(int attemptNumber, int capSeconds)
    {
        double delayMs = PollBaseDelayMs * Math.Pow(PollBackoffMultiplier, Math.Max(0, attemptNumber - 1));
        double cappedMs = Math.Min(delayMs, capSeconds * 1000d);
        return TimeSpan.FromMilliseconds(cappedMs);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found {Count} pending submission jobs.")]
    private partial void LogFound(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing step '{StepName}' ({StepType}) for job {JobId}")]
    private partial void LogExecutingStep(Guid jobId, string stepName, string stepType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobId} advanced to step '{StepName}'")]
    private partial void LogAdvanced(Guid jobId, string stepName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job {JobId} completed successfully.")]
    private partial void LogCompleted(Guid jobId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {JobId} step '{StepName}' failed permanently: {Error}")]
    private partial void LogStepFailed(Guid jobId, string stepName, string? error);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Job {JobId} step '{StepName}' attempt {Attempt}/{Max} failed; will retry."
    )]
    private partial void LogStepRetrying(Guid jobId, string stepName, int attempt, int max);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing job {JobId}")]
    private partial void LogJobFailed(Guid jobId, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Job {JobId} was already processed by another worker — skipping.")]
    private partial void LogJobAlreadyProcessed(Guid jobId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Job {JobId} is currently locked by another worker — skipping.")]
    private partial void LogJobAlreadyLocked(Guid jobId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No submission job found for submission {SubmissionId}.")]
    private partial void LogSubmissionJobNotFound(Guid submissionId);
}