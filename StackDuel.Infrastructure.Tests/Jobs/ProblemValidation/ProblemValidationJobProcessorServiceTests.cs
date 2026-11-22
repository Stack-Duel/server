using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Messaging;
using StackDuel.Application.Services.Problems;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Infrastructure.Jobs.ProblemValidation;
using StackDuel.Infrastructure.ProblemValidation;

namespace StackDuel.Infrastructure.Tests.Jobs.ProblemValidation;

public class ProblemValidationJobProcessorServiceTests
{
    private const int MaxProcessingAttempts = 5;

    private readonly Mock<IProblemValidationJobRepository> _jobRepository = new();
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly StubReferenceSolutionValidationService _validationService = new();
    private readonly Mock<ITestCaseGenerationJobRepository> _generationJobRepository = new();
    private readonly Mock<IMessagePublisher> _messagePublisher = new();
    private readonly Mock<ISlugService> _slugService = new();
    private readonly ProblemValidationJobProcessorService _processor;

    public ProblemValidationJobProcessorServiceTests()
    {
        ServiceCollection services = new();
        services.AddSingleton(_jobRepository.Object);
        services.AddSingleton(_problemRepository.Object);
        services.AddSingleton<IReferenceSolutionValidationService>(_validationService);
        services.AddSingleton(_generationJobRepository.Object);
        services.AddSingleton(_messagePublisher.Object);
        services.AddSingleton(_slugService.Object);

        ServiceProvider provider = services.BuildServiceProvider();

        _processor = new ProblemValidationJobProcessorService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ProblemValidationJobProcessorService>.Instance
        );

        _jobRepository
            .Setup(repository =>
                repository.TryClaimAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);

        _jobRepository
            .Setup(repository => repository.FindPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _jobRepository
            .Setup(repository =>
                repository.FindAwaitingGenerationAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);

        _jobRepository
            .Setup(repository => repository.IsCancelledAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private static Problem CreateProblem()
    {
        Problem problem = new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('q', 60)),
            new Difficulty(150),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );
        problem.SubmitForValidation();
        return problem;
    }

    private ProblemValidationJob SetUpPendingJob(Problem? problem = null)
    {
        Problem resolved = problem ?? CreateProblem();
        ProblemValidationJob job = new(resolved.Id);

        _jobRepository
            .Setup(repository => repository.FindByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(resolved.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        return job;
    }

    private void SetUpValidationOutcome(bool allPassed, params SetupValidationOutcome[] outcomes) =>
        _validationService.Returns(allPassed, outcomes);

    [Fact]
    public async Task AJobLockedByAnotherWorker_IsLeftAlone()
    {
        ProblemValidationJob job = SetUpPendingJob();
        _jobRepository
            .Setup(repository =>
                repository.TryClaimAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(false);

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        _jobRepository.Verify(
            repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _jobRepository.Verify(
            repository => repository.ReleaseLockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AJobThatDisappeared_ReleasesTheLockWithoutWork()
    {
        Guid jobId = Guid.NewGuid();
        _jobRepository
            .Setup(repository => repository.FindByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemValidationJob?)null);

        await _processor.RunPendingForJobAsync(jobId, CancellationToken.None);

        Assert.Equal(0, _validationService.CallCount);
        _jobRepository.Verify(repository => repository.ReleaseLockAsync(jobId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AJobAlreadyPastPending_IsNotProcessedAgain()
    {
        ProblemValidationJob job = SetUpPendingJob();
        job.MoveToAwaitingGeneration([Guid.NewGuid()]);

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal(0, _validationService.CallCount);
        _jobRepository.Verify(
            repository => repository.ReleaseLockAsync(job.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AJobWhoseProblemWasDeleted_FailsWithoutRunningJudge0()
    {
        ProblemValidationJob job = SetUpPendingJob();
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal(ProblemValidationJobStatus.Failed, job.Status);
        Assert.Equal("Problem no longer exists.", job.FailureReason);
        Assert.Equal(0, _validationService.CallCount);
        _jobRepository.Verify(repository => repository.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AJobCancelledWhileJudge0WasRunning_IsAbandonedBeforeAnythingIsWritten()
    {
        ProblemValidationJob job = SetUpPendingJob();
        SetUpValidationOutcome(true, new SetupValidationOutcome(Guid.NewGuid(), "javascript", true, []));
        _jobRepository
            .Setup(repository => repository.IsCancelledAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal(ProblemValidationJobStatus.Pending, job.Status);
        _jobRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<ProblemValidationJob>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _generationJobRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<StackDuel.Domain.TestCaseGeneration.Entities.TestCaseGenerationJob>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task AJobWhoseCancellationCannotBeDetermined_IsTreatedAsCancelled()
    {
        ProblemValidationJob job = SetUpPendingJob();
        SetUpValidationOutcome(true, new SetupValidationOutcome(Guid.NewGuid(), "javascript", true, []));
        _jobRepository
            .Setup(repository => repository.IsCancelledAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal(ProblemValidationJobStatus.Pending, job.Status);
        _jobRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<ProblemValidationJob>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task TheAuthorChecksStep_SucceedsBeforeReferenceSolutionsRun()
    {
        ProblemValidationJob job = SetUpPendingJob();
        SetUpValidationOutcome(true, new SetupValidationOutcome(Guid.NewGuid(), "javascript", true, []));
        _jobRepository
            .Setup(repository => repository.IsCancelledAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        ProblemValidationStep authorChecks = job.Steps.Single(step =>
            step.Kind == ProblemValidationStepKind.AuthorChecks
        );
        Assert.Equal(ProblemValidationStepStatus.Succeeded, authorChecks.Status);
        Assert.Equal(
            ProblemValidationStepStatus.Running,
            job.Steps.Single(step => step.Kind == ProblemValidationStepKind.ReferenceSolutions).Status
        );
    }

    [Fact]
    public async Task AThrowingJob_RecordsTheAttemptAndSchedulesARetryWithoutReleasingTheLock()
    {
        ProblemValidationJob job = SetUpPendingJob();
        _validationService.Throws(new InvalidOperationException("judge0 unreachable"));

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal(1, job.ProcessingAttempts);
        Assert.Equal("judge0 unreachable", job.LastError);
        Assert.Equal(ProblemValidationJobStatus.Pending, job.Status);
        _jobRepository.Verify(
            repository =>
                repository.SetRetryAfterAsync(job.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _jobRepository.Verify(
            repository => repository.ReleaseLockAsync(job.Id, It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task TheRetryBackoff_GrowsWithEachAttempt()
    {
        ProblemValidationJob job = SetUpPendingJob();
        _validationService.Throws(new InvalidOperationException("judge0 unreachable"));

        List<TimeSpan> delays = [];
        _jobRepository
            .Setup(repository =>
                repository.SetRetryAfterAsync(job.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())
            )
            .Callback((Guid _, TimeSpan delay, CancellationToken _) => delays.Add(delay))
            .Returns(Task.CompletedTask);

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);
        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);
        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal([TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120)], delays);
    }

    [Fact]
    public async Task AJobThatKeepsThrowing_GivesUpAndFailsTheProblem()
    {
        Problem problem = CreateProblem();
        ProblemValidationJob job = SetUpPendingJob(problem);
        _validationService.Throws(new InvalidOperationException("judge0 unreachable"));

        for (int attempt = 0; attempt < MaxProcessingAttempts; attempt++)
            await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal(ProblemValidationJobStatus.Failed, job.Status);
        Assert.Equal(ProblemStatus.Failed, problem.Status);
        Assert.Contains("judge0 unreachable", problem.ValidationFailureReason);
        _jobRepository.Verify(
            repository => repository.ReleaseLockAsync(job.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _jobRepository.Verify(
            repository =>
                repository.SetRetryAfterAsync(job.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Exactly(MaxProcessingAttempts - 1)
        );
    }

    [Fact]
    public async Task AWrappedException_ReportsTheInnermostMessageAlongsideTheOuterOne()
    {
        ProblemValidationJob job = SetUpPendingJob();
        _validationService.Throws(
            new InvalidOperationException(
                "could not save",
                new InvalidOperationException("value too long for column")
            )
        );

        await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal("could not save -> value too long for column", job.LastError);
    }

    [Fact]
    public async Task AThrowingJobWhoseProblemVanished_StillRecordsTheAttempt()
    {
        ProblemValidationJob job = SetUpPendingJob();
        _validationService.Throws(new InvalidOperationException("judge0 unreachable"));
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        for (int attempt = 0; attempt < MaxProcessingAttempts; attempt++)
            await _processor.RunPendingForJobAsync(job.Id, CancellationToken.None);

        Assert.Equal(ProblemValidationJobStatus.Failed, job.Status);
        _problemRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AFailureRecordedForAJobThatVanished_IsDroppedQuietly()
    {
        Guid jobId = Guid.NewGuid();
        ProblemValidationJob job = SetUpPendingJob();

        _jobRepository
            .Setup(repository => repository.FindByIdAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemValidationJob?)null);

        await _processor.RunPendingForJobAsync(jobId, CancellationToken.None);

        Assert.Equal(0, job.ProcessingAttempts);
    }

    [Fact]
    public async Task ACancellationRequestedByTheHost_PropagatesRatherThanCountingAsAnAttempt()
    {
        ProblemValidationJob job = SetUpPendingJob();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        _validationService.Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _processor.RunPendingForJobAsync(job.Id, cancellation.Token)
        );

        Assert.Equal(0, job.ProcessingAttempts);
    }

    [Fact]
    public async Task ARunPass_ClaimsEveryPendingAndAwaitingGenerationJobItFinds()
    {
        ProblemValidationJob pending = SetUpPendingJob();
        ProblemValidationJob awaiting = SetUpPendingJob();
        awaiting.MoveToAwaitingGeneration([]);

        _jobRepository
            .Setup(repository => repository.FindPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([pending]);
        _jobRepository
            .Setup(repository =>
                repository.FindAwaitingGenerationAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([awaiting]);
        SetUpValidationOutcome(true, new SetupValidationOutcome(Guid.NewGuid(), "javascript", true, []));
        _jobRepository
            .Setup(repository => repository.IsCancelledAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _processor.RunAsync(CancellationToken.None);

        _jobRepository.Verify(
            repository => repository.TryClaimAsync(pending.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _jobRepository.Verify(
            repository =>
                repository.TryClaimAsync(awaiting.Id, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AnEmptyQueue_DoesNoWork()
    {
        await _processor.RunAsync(CancellationToken.None);

        _jobRepository.Verify(
            repository =>
                repository.TryClaimAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AnAwaitingGenerationPass_SkipsAJobStillSittingInPending()
    {
        ProblemValidationJob job = SetUpPendingJob();
        _jobRepository
            .Setup(repository =>
                repository.FindAwaitingGenerationAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([job]);

        await _processor.RunAwaitingGenerationPassAsync(CancellationToken.None);

        _generationJobRepository.Verify(
            repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _jobRepository.Verify(
            repository => repository.ReleaseLockAsync(job.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}