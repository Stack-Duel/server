using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Application.Services.Problems;
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

namespace StackDuel.Infrastructure.Jobs.ProblemValidation;

internal sealed partial class ProblemValidationJobProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<ProblemValidationJobProcessorService> logger
)
{
    private const int BatchSize = 5;
    private const string ProblemNoLongerExistsMessage = "Problem no longer exists.";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private const int MaxProcessingAttempts = 5;
    private const int InitialBackoffSeconds = 30;
    private const int MaxBackoffSeconds = 900;

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

        bool releaseLock = true;

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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            releaseLock = false;
            await RecordAttemptFailureAsync(jobId, exception, CancellationToken.None);
        }
        finally
        {
            if (releaseLock)
                await jobRepository.ReleaseLockAsync(jobId, CancellationToken.None);
        }
    }

    private async Task RecordAttemptFailureAsync(Guid jobId, Exception exception, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        var jobRepository = sp.GetRequiredService<IProblemValidationJobRepository>();

        ProblemValidationJob? job = await jobRepository.FindByIdAsync(jobId, ct);

        if (job is null)
            return;

        string reason = DescribeException(exception);

        job.RecordAttemptFailure(reason, MaxProcessingAttempts);

        if (job.Status == ProblemValidationJobStatus.Failed)
        {
            var problemRepository = sp.GetRequiredService<IProblemRepository>();
            Problem? problem = await problemRepository.FindByIdAsync(job.ProblemId, ct);

            if (problem is not null)
            {
                problem.FailValidation(job.FailureReason ?? reason);
                await problemRepository.UpdateAsync(problem, ct);
            }

            await jobRepository.UpdateAsync(job, ct);
            await jobRepository.ReleaseLockAsync(jobId, ct);
            LogGaveUp(jobId, job.ProcessingAttempts, reason);
            return;
        }

        await jobRepository.UpdateAsync(job, ct);
        await jobRepository.SetRetryAfterAsync(jobId, BackoffFor(job.ProcessingAttempts), ct);
        LogAttemptFailed(jobId, job.ProcessingAttempts, reason);
    }

    private static string DescribeException(Exception exception)
    {
        Exception innermost = exception;

        while (innermost.InnerException is not null)
            innermost = innermost.InnerException;

        return ReferenceEquals(innermost, exception)
            ? exception.Message
            : $"{exception.Message} -> {innermost.Message}";
    }

    private static async Task<bool> WasCancelledAsync(
        IProblemValidationJobRepository jobRepository,
        Guid jobId,
        CancellationToken ct
    )
    {
        bool? isCancelled = await jobRepository.IsCancelledAsync(jobId, ct);

        return isCancelled ?? true;
    }

    private static async Task AssignPublishedSlugAsync(IServiceProvider sp, Problem problem, CancellationToken ct)
    {
        if (!problem.Slug.IsDraftSlug)
            return;

        var slugService = sp.GetRequiredService<ISlugService>();

        problem.UpdateSlug(await slugService.ResolveUniqueSlugFromTitleAsync(problem.Title, ct));
    }

    private static TimeSpan BackoffFor(int attempts) =>
        TimeSpan.FromSeconds(Math.Min(InitialBackoffSeconds * Math.Pow(2, attempts - 1), MaxBackoffSeconds));

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

        job.SucceedStep(
            ProblemValidationStepKind.AuthorChecks,
            $"{problem.ActiveSetups().Count()} setup(s) and generation parameters present."
        );
        job.BeginStep(ProblemValidationStepKind.ReferenceSolutions);

        var validationService = sp.GetRequiredService<IReferenceSolutionValidationService>();
        ReferenceSolutionValidationResult result = await validationService.ValidateAsync(job.ProblemId, ct);

        IReadOnlyList<ProblemValidationStepItemSpec> referenceSolutionItems =
        [
            .. result.Outcomes.Select(outcome => new ProblemValidationStepItemSpec(
                outcome.LanguageLabel,
                outcome.Passed ? ProblemValidationStepStatus.Succeeded : ProblemValidationStepStatus.Failed,
                outcome.Warnings.Count > 0 ? string.Join("; ", outcome.Warnings) : null
            )),
        ];

        if (await WasCancelledAsync(jobRepository, jobId, ct))
        {
            LogAbandonedAfterCancel(jobId);
            return;
        }

        var db = sp.GetRequiredService<StackDuelDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        if (!result.AllPassed)
        {
            string summary = result.BuildFailureSummary();

            problem.FailValidation(summary);
            await problemRepository.UpdateAsync(problem, ct);

            job.FailStep(ProblemValidationStepKind.ReferenceSolutions, summary, referenceSolutionItems);
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

        job.SucceedStep(
            ProblemValidationStepKind.ReferenceSolutions,
            $"{result.Outcomes.Count} of {result.Outcomes.Count} passed.",
            referenceSolutionItems
        );
        job.BeginStep(ProblemValidationStepKind.TestCaseGeneration);

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

        if (await WasCancelledAsync(jobRepository, jobId, ct))
        {
            LogAbandonedAfterCancel(jobId);
            return;
        }

        var db = sp.GetRequiredService<StackDuelDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        List<TestCaseGenerationJob> failedGenerationJobs =
        [
            .. generationJobs.Where(g => g.Status == TestCaseGenerationJobStatus.Failed),
        ];

        IReadOnlyList<ProblemValidationStepItemSpec> generationItems = BuildGenerationItems(generationJobs, problem);

        if (failedGenerationJobs.Count > 0)
        {
            string reason = BuildGenerationFailureReason(failedGenerationJobs);

            problem.FailValidation(reason);
            await problemRepository.UpdateAsync(problem, ct);

            job.FailStep(ProblemValidationStepKind.TestCaseGeneration, reason, generationItems);
            job.Fail(reason);
            await jobRepository.UpdateAsync(job, ct);

            await transaction.CommitAsync(ct);
            LogFailed(jobId, reason);
            return;
        }

        string summary = $"Generated verified test cases for {generationJobs.Count} setup(s).";

        await AssignPublishedSlugAsync(sp, problem, ct);

        problem.CompleteValidation();
        await problemRepository.UpdateAsync(problem, ct);

        job.SucceedStep(ProblemValidationStepKind.TestCaseGeneration, summary, generationItems);
        job.BeginStep(ProblemValidationStepKind.Publish);
        job.Complete(summary);
        await jobRepository.UpdateAsync(job, ct);

        await transaction.CommitAsync(ct);
        LogCompleted(jobId, summary);
    }

    private const int MaxGenerationFailuresReported = 3;
    private const int MaxGenerationFailureDetailLength = 600;

    private static string BuildGenerationFailureReason(IReadOnlyList<TestCaseGenerationJob> failedGenerationJobs)
    {
        string details = string.Join(
            "; ",
            failedGenerationJobs
                .Take(MaxGenerationFailuresReported)
                .Select(generationJob => Shorten(generationJob.FailureReason ?? "unknown error"))
        );

        if (failedGenerationJobs.Count > MaxGenerationFailuresReported)
            details += $" (+{failedGenerationJobs.Count - MaxGenerationFailuresReported} more)";

        return $"Test case generation failed for {failedGenerationJobs.Count} setup(s): {details}";
    }

    private static string Shorten(string value) =>
        value.Length > MaxGenerationFailureDetailLength ? value[..MaxGenerationFailureDetailLength] + "..." : value;

    private static IReadOnlyList<ProblemValidationStepItemSpec> BuildGenerationItems(
        IReadOnlyList<TestCaseGenerationJob> generationJobs,
        Problem problem
    )
    {
        Dictionary<Guid, string> setupLabels = problem.Setups.ToDictionary(
            setup => setup.Id,
            setup => setup.FunctionName ?? setup.Id.ToString()
        );

        return
        [
            .. generationJobs.Select(generationJob => new ProblemValidationStepItemSpec(
                setupLabels.TryGetValue(generationJob.ProblemSetupId, out string? label) ? label : "Setup",
                generationJob.Status switch
                {
                    TestCaseGenerationJobStatus.Completed => ProblemValidationStepStatus.Succeeded,
                    TestCaseGenerationJobStatus.Failed => ProblemValidationStepStatus.Failed,
                    _ => ProblemValidationStepStatus.Running,
                },
                generationJob.FailureReason ?? generationJob.ResultSummary
            )),
        ];
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Problem validation job {JobId} was cancelled; abandoning this pass"
    )]
    private partial void LogAbandonedAfterCancel(Guid jobId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Problem validation job {JobId} attempt {Attempts} failed, will retry: {Reason}"
    )]
    private partial void LogAttemptFailed(Guid jobId, int attempts, string reason);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Problem validation job {JobId} gave up after {Attempts} attempts: {Reason}"
    )]
    private partial void LogGaveUp(Guid jobId, int attempts, string reason);

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