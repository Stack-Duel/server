using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Jobs.Submissions;
using StackDuel.Application.Submissions;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.IntegrationTests.Infrastructure;

namespace StackDuel.IntegrationTests.Submissions;

public sealed class SubmissionCleanupServiceTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static DateTime StaleCreatedAt =>
        DateTime.UtcNow - SubmissionGradingWindow.StaleAfter.Add(TimeSpan.FromMinutes(5));

    private static DateTime FreshCreatedAt =>
        DateTime.UtcNow - SubmissionGradingWindow.StaleAfter.Subtract(TimeSpan.FromMinutes(5));

    [Theory]
    [InlineData(SubmissionJobStatus.Pending)]
    [InlineData(SubmissionJobStatus.Running)]
    public async Task RunAsync_fails_a_stale_job_and_drives_its_submission_to_a_terminal_status(
        SubmissionJobStatus jobStatus
    )
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync($"sub-stale-{jobStatus}", $"stale{(int)jobStatus}");
        Guid submissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            status: SubmissionStatus.Queued
        );
        Guid jobId = await Seeder.CreateSubmissionJobAsync(submissionId, fixture.PipelineId, jobStatus, StaleCreatedAt);

        await RunCleanupAsync();

        SeededSubmissionJob job = await Seeder.GetSubmissionJobAsync(jobId);
        Assert.Equal(SubmissionJobStatus.Failed, job.Status);
        Assert.False(string.IsNullOrWhiteSpace(job.FailureReason));
        Assert.True(job.CompletedAt.HasValue);
        Assert.Equal(SubmissionStatus.WrongAnswer, await Seeder.GetSubmissionStatusAsync(submissionId));
    }

    [Fact]
    public async Task RunAsync_leaves_a_job_younger_than_the_stale_threshold_untouched()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync("sub-fresh", "fresh");
        Guid submissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            status: SubmissionStatus.Running
        );
        Guid jobId = await Seeder.CreateSubmissionJobAsync(
            submissionId,
            fixture.PipelineId,
            SubmissionJobStatus.Running,
            FreshCreatedAt
        );

        await RunCleanupAsync();

        SeededSubmissionJob job = await Seeder.GetSubmissionJobAsync(jobId);
        Assert.Equal(SubmissionJobStatus.Running, job.Status);
        Assert.Null(job.FailureReason);
        Assert.Null(job.CompletedAt);
        Assert.Equal(SubmissionStatus.Running, await Seeder.GetSubmissionStatusAsync(submissionId));
    }

    [Fact]
    public async Task RunAsync_never_downgrades_an_already_accepted_submission()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync("sub-accepted", "accepted");
        Guid submissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            status: SubmissionStatus.Accepted
        );
        Guid jobId = await Seeder.CreateSubmissionJobAsync(
            submissionId,
            fixture.PipelineId,
            SubmissionJobStatus.Running,
            StaleCreatedAt
        );

        await RunCleanupAsync();

        SeededSubmissionJob job = await Seeder.GetSubmissionJobAsync(jobId);
        Assert.Equal(SubmissionJobStatus.Failed, job.Status);
        Assert.Equal(SubmissionStatus.Accepted, await Seeder.GetSubmissionStatusAsync(submissionId));
    }

    [Fact]
    public async Task RunAsync_drives_every_stale_jobs_submission_to_a_terminal_status()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync(problemCount: 2);
        Guid userId = await Seeder.CreateUserAsync("sub-sweeper", "sweeper");

        Guid firstSubmissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problems[0].ProblemSetupId,
            status: SubmissionStatus.Queued
        );
        Guid secondSubmissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problems[1].ProblemSetupId,
            status: SubmissionStatus.Running
        );

        Guid firstJobId = await Seeder.CreateSubmissionJobAsync(
            firstSubmissionId,
            fixture.PipelineId,
            SubmissionJobStatus.Pending,
            StaleCreatedAt
        );
        Guid secondJobId = await Seeder.CreateSubmissionJobAsync(
            secondSubmissionId,
            fixture.PipelineId,
            SubmissionJobStatus.Running,
            StaleCreatedAt
        );

        await RunCleanupAsync();

        Assert.Equal(SubmissionJobStatus.Failed, (await Seeder.GetSubmissionJobAsync(firstJobId)).Status);
        Assert.Equal(SubmissionJobStatus.Failed, (await Seeder.GetSubmissionJobAsync(secondJobId)).Status);
        Assert.Equal(SubmissionStatus.WrongAnswer, await Seeder.GetSubmissionStatusAsync(firstSubmissionId));
        Assert.Equal(SubmissionStatus.WrongAnswer, await Seeder.GetSubmissionStatusAsync(secondSubmissionId));
    }

    private async Task RunCleanupAsync()
    {
        using IServiceScope scope = CreateServiceScope();
        ISubmissionCleanupService cleanupService =
            scope.ServiceProvider.GetRequiredService<ISubmissionCleanupService>();
        await cleanupService.RunAsync();
    }
}