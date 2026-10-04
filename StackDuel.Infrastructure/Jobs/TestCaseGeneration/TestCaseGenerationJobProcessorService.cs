using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Application.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.Jobs.TestCaseGeneration;

/// <summary>
/// Picks up pending <see cref="TestCaseGenerationJob"/> entries and runs generation for each.
/// Creates a new DI scope per job so each job gets its own isolated DbContext — same pattern
/// as SubmissionJobProcessorService, minus the multi-step machinery, since generation is a
/// single-shot operation rather than a pipeline.
/// </summary>
internal sealed partial class TestCaseGenerationJobProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<TestCaseGenerationJobProcessorService> logger
)
{
    // Generation makes many Judge0 round trips per job (a full batch of attempts), so batches
    // are kept small relative to SubmissionJobProcessorService's — this isn't latency-sensitive.
    private const int BatchSize = 5;

    // Must comfortably exceed how long a single problem's generation can take (attempts up to
    // TargetCaseCount * 15, in rounds — see TestCaseGenerationService). If a worker crashes
    // mid-run the lease simply expires and another sweep picks it back up.
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(10);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TestCaseGenerationJob> jobs;
        using (var scope = scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ITestCaseGenerationJobRepository>();
            jobs = await repo.FindPendingAsync(BatchSize, cancellationToken);
        }

        LogFound(jobs.Count);

        foreach (var job in jobs)
        {
            await ProcessInNewScopeAsync(job.Id, cancellationToken);
        }
    }

    public async Task RunForJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        await ProcessInNewScopeAsync(jobId, cancellationToken);

    private async Task ProcessInNewScopeAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var jobRepository = sp.GetRequiredService<ITestCaseGenerationJobRepository>();
        var messagePublisher = sp.GetRequiredService<IMessagePublisher>();

        bool claimed = await jobRepository.TryClaimAsync(jobId, LeaseDuration, ct);
        if (!claimed)
        {
            LogJobAlreadyLocked(jobId);
            return;
        }

        try
        {
            var job = await jobRepository.FindByIdAsync(jobId, ct);
            if (job is null)
            {
                LogJobNotFound(jobId);
                return;
            }

            if (job.Status != TestCaseGenerationJobStatus.Pending)
            {
                LogJobAlreadyProcessed(jobId);
                return;
            }

            var generationService = sp.GetRequiredService<ITestCaseGenerationService>();

            try
            {
                GenerateTestCasesResult result = await generationService.GenerateAsync(
                    new GenerateTestCasesRequest(
                        job.ProblemSetupId,
                        job.ReferenceSolutionCode,
                        job.Parameters,
                        job.OutputValueType,
                        job.TargetCaseCount,
                        job.Seed
                    ),
                    ct
                );

                string summary =
                    $"Generated {result.Generated}/{result.Requested} "
                    + $"({result.Skipped} attempts skipped, {result.Warnings.Count} warnings).";

                // A job that produced at least some verified cases is a completion, even if it
                // fell short of the target (result.Succeeded is the stricter "hit target
                // exactly" signal) — only zero output (e.g. the sanity check rejected the
                // reference solution) counts as a real failure.
                if (result.Generated > 0)
                    job.Complete(summary);
                else
                    job.Fail(summary);

                await jobRepository.UpdateAsync(job, ct);
                LogCompleted(jobId, summary);
            }
            catch (Exception ex)
            {
                job.Fail($"Unhandled error: {ex.Message}");
                await jobRepository.UpdateAsync(job, ct);
                LogFailed(jobId, ex);
            }

            await messagePublisher.PublishAsync(
                new TestCaseGenerationJobCompletedMessage(jobId),
                CancellationToken.None
            );
        }
        finally
        {
            await jobRepository.ReleaseLockAsync(jobId, CancellationToken.None);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} pending test case generation job(s)")]
    private partial void LogFound(int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Test case generation job {JobId} is already locked by another worker"
    )]
    private partial void LogJobAlreadyLocked(Guid jobId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Test case generation job {JobId} not found when processing in isolated scope"
    )]
    private partial void LogJobNotFound(Guid jobId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Test case generation job {JobId} was already processed")]
    private partial void LogJobAlreadyProcessed(Guid jobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Test case generation job {JobId} completed: {Summary}")]
    private partial void LogCompleted(Guid jobId, string summary);

    [LoggerMessage(Level = LogLevel.Error, Message = "Test case generation job {JobId} failed")]
    private partial void LogFailed(Guid jobId, Exception exception);
}