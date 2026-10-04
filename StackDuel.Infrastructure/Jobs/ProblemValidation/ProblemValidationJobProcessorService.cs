using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.Enums;
using StackDuel.Infrastructure.Persistence;
using StackDuel.Infrastructure.ProblemValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.Jobs.ProblemValidation;

/// <summary>
/// Drives a <see cref="ProblemValidationJob"/> through its two passes: validate every required
/// language's reference solution (<see cref="IReferenceSolutionValidationService"/>), then, once
/// all pass, enqueue a <see cref="TestCaseGenerationJob"/> per setup and wait for those to finish
/// before publishing the problem. Same per-job-scope/claim/release structure as
/// <see cref="TestCaseGeneration.TestCaseGenerationJobProcessorService"/>, chaining into that
/// existing, unmodified pipeline for the actual generation work rather than reimplementing it.
/// </summary>
internal sealed partial class ProblemValidationJobProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<ProblemValidationJobProcessorService> logger
)
{
    private const int BatchSize = 5;
    private const string ProblemNoLongerExistsMessage = "Problem no longer exists.";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await RunPendingPassAsync(cancellationToken);
        await RunAwaitingGenerationPassAsync(cancellationToken);
    }

    public async Task RunPendingForJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        await ProcessPendingInNewScopeAsync(jobId, cancellationToken);

    public async Task RunAwaitingGenerationPassAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProblemValidationJob> jobs;
        using (var scope = scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProblemValidationJobRepository>();
            jobs = await repo.FindAwaitingGenerationAsync(BatchSize, cancellationToken);
        }

        LogFoundAwaitingGeneration(jobs.Count);

        foreach (ProblemValidationJob job in jobs)
            await ProcessAwaitingGenerationInNewScopeAsync(job.Id, cancellationToken);
    }

    private async Task RunPendingPassAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProblemValidationJob> jobs;
        using (var scope = scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IProblemValidationJobRepository>();
            jobs = await repo.FindPendingAsync(BatchSize, cancellationToken);
        }

        LogFoundPending(jobs.Count);

        foreach (ProblemValidationJob job in jobs)
            await ProcessPendingInNewScopeAsync(job.Id, cancellationToken);
    }

    private async Task ProcessPendingInNewScopeAsync(Guid jobId, CancellationToken ct) =>
        await ProcessInClaimedScopeAsync(jobId, ProblemValidationJobStatus.Pending, HandlePendingJobAsync, ct);

    private async Task ProcessAwaitingGenerationInNewScopeAsync(Guid jobId, CancellationToken ct) =>
        await ProcessInClaimedScopeAsync(
            jobId,
            ProblemValidationJobStatus.AwaitingGeneration,
            HandleAwaitingGenerationJobAsync,
            ct
        );

    /// <summary>
    /// Shared claim/load/status-check/release scaffolding for both passes. Creates a fresh DI
    /// scope and DbContext per job, atomically claims the job's processing lock, loads it, verifies
    /// it's still in the expected status (another worker may have already advanced it), then hands
    /// off to the pass-specific handler.
    /// </summary>
    private async Task ProcessInClaimedScopeAsync(
        Guid jobId,
        ProblemValidationJobStatus expectedStatus,
        Func<IServiceProvider, ProblemValidationJob, CancellationToken, Task> handle,
        CancellationToken ct
    )
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var jobRepository = sp.GetRequiredService<IProblemValidationJobRepository>();

        bool claimed = await jobRepository.TryClaimAsync(jobId, LeaseDuration, ct);
        if (!claimed)
        {
            LogJobAlreadyLocked(jobId);
            return;
        }

        try
        {
            ProblemValidationJob? job = await jobRepository.FindByIdAsync(jobId, ct);
            if (job is null)
            {
                LogJobNotFound(jobId);
                return;
            }

            if (job.Status != expectedStatus)
            {
                LogJobAlreadyProcessed(jobId);
                return;
            }

            await handle(sp, job, ct);
        }
        finally
        {
            await jobRepository.ReleaseLockAsync(jobId, CancellationToken.None);
        }
    }

    private async Task HandlePendingJobAsync(IServiceProvider sp, ProblemValidationJob job, CancellationToken ct)
    {
        Guid jobId = job.Id;
        var jobRepository = sp.GetRequiredService<IProblemValidationJobRepository>();
        var messagePublisher = sp.GetRequiredService<IMessagePublisher>();

        var problemRepository = sp.GetRequiredService<IProblemRepository>();
        Problem? problem = await problemRepository.FindByIdAsync(job.ProblemId, ct);

        if (problem is null)
        {
            job.Fail(ProblemNoLongerExistsMessage);
            await jobRepository.UpdateAsync(job, ct);
            LogFailed(jobId, ProblemNoLongerExistsMessage);
            return;
        }

        var validationService = sp.GetRequiredService<IReferenceSolutionValidationService>();
        ReferenceSolutionValidationResult result = await validationService.ValidateAsync(job.ProblemId, ct);

        var db = sp.GetRequiredService<StackDuelDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        if (!result.AllPassed)
        {
            string summary = result.BuildFailureSummary();

            problem.FailValidation(summary);
            await problemRepository.UpdateAsync(problem, ct);

            job.Fail(summary);
            await jobRepository.UpdateAsync(job, ct);

            await transaction.CommitAsync(ct);
            LogFailed(jobId, summary);
            return;
        }

        var testCaseGenerationJobRepository = sp.GetRequiredService<ITestCaseGenerationJobRepository>();
        List<Guid> generationJobIds = [];

        ProblemGenerationSpec generationSpec = problem.GenerationSpec!;

        foreach (SetupValidationOutcome outcome in result.Outcomes)
        {
            ProblemSetup setup = problem.Setups.First(s => s.Id == outcome.ProblemSetupId);

            TestCaseGenerationJob generationJob = new(
                setup.Id,
                setup.ReferenceSolutionCode!,
                generationSpec.Parameters,
                generationSpec.OutputValueType,
                generationSpec.TargetCaseCount,
                generationSpec.Seed
            );

            await testCaseGenerationJobRepository.AddAsync(generationJob, ct);
            generationJobIds.Add(generationJob.Id);
        }

        job.MoveToAwaitingGeneration(generationJobIds);
        await jobRepository.UpdateAsync(job, ct);

        await transaction.CommitAsync(ct);
        LogValidationPassed(jobId, generationJobIds.Count);

        foreach (Guid generationJobId in generationJobIds)
            await messagePublisher.PublishAsync(
                new TestCaseGenerationJobContinuationMessage(generationJobId),
                CancellationToken.None
            );
    }

    private async Task HandleAwaitingGenerationJobAsync(
        IServiceProvider sp,
        ProblemValidationJob job,
        CancellationToken ct
    )
    {
        Guid jobId = job.Id;
        var jobRepository = sp.GetRequiredService<IProblemValidationJobRepository>();
        var testCaseGenerationJobRepository = sp.GetRequiredService<ITestCaseGenerationJobRepository>();

        List<TestCaseGenerationJob> generationJobs = [];
        foreach (Guid generationJobId in job.TestCaseGenerationJobIds)
        {
            TestCaseGenerationJob? generationJob = await testCaseGenerationJobRepository.FindByIdAsync(
                generationJobId,
                ct
            );
            if (generationJob is not null)
                generationJobs.Add(generationJob);
        }

        if (generationJobs.Any(g => g.Status == TestCaseGenerationJobStatus.Pending))
        {
            LogStillAwaitingGeneration(jobId);
            return;
        }

        var problemRepository = sp.GetRequiredService<IProblemRepository>();
        Problem? problem = await problemRepository.FindByIdAsync(job.ProblemId, ct);

        if (problem is null)
        {
            job.Fail(ProblemNoLongerExistsMessage);
            await jobRepository.UpdateAsync(job, ct);
            LogFailed(jobId, ProblemNoLongerExistsMessage);
            return;
        }

        var db = sp.GetRequiredService<StackDuelDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        List<TestCaseGenerationJob> failedGenerationJobs =
        [
            .. generationJobs.Where(g => g.Status == TestCaseGenerationJobStatus.Failed),
        ];

        if (failedGenerationJobs.Count > 0)
        {
            string reason =
                $"Test case generation failed for {failedGenerationJobs.Count} setup(s): "
                + string.Join("; ", failedGenerationJobs.Select(g => g.FailureReason ?? "unknown error"));

            problem.FailValidation(reason);
            await problemRepository.UpdateAsync(problem, ct);

            job.Fail(reason);
            await jobRepository.UpdateAsync(job, ct);

            await transaction.CommitAsync(ct);
            LogFailed(jobId, reason);
            return;
        }

        string summary = $"Generated verified test cases for {generationJobs.Count} setup(s).";

        problem.CompleteValidation();
        await problemRepository.UpdateAsync(problem, ct);

        job.Complete(summary);
        await jobRepository.UpdateAsync(job, ct);

        await transaction.CommitAsync(ct);
        LogCompleted(jobId, summary);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} pending problem validation job(s)")]
    private partial void LogFoundPending(int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Found {Count} problem validation job(s) awaiting generation"
    )]
    private partial void LogFoundAwaitingGeneration(int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Problem validation job {JobId} is already locked by another worker"
    )]
    private partial void LogJobAlreadyLocked(Guid jobId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Problem validation job {JobId} not found when processing in isolated scope"
    )]
    private partial void LogJobNotFound(Guid jobId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Problem validation job {JobId} was already processed")]
    private partial void LogJobAlreadyProcessed(Guid jobId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Problem validation job {JobId} is still awaiting test case generation"
    )]
    private partial void LogStillAwaitingGeneration(Guid jobId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Problem validation job {JobId} passed validation, enqueued {Count} generation job(s)"
    )]
    private partial void LogValidationPassed(Guid jobId, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Problem validation job {JobId} completed: {Summary}")]
    private partial void LogCompleted(Guid jobId, string summary);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Problem validation job {JobId} failed: {Reason}")]
    private partial void LogFailed(Guid jobId, string reason);
}