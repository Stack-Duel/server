using Microsoft.Extensions.DependencyInjection;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProblemValidationJobCancellationTests(IntegrationTestEnvironment environment)
{
    private async Task<T> WithRepositoryAsync<T>(Func<IProblemValidationJobRepository, Task<T>> work)
    {
        await using AsyncServiceScope scope = environment.Factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProblemValidationJobRepository>();

        return await work(repository);
    }

    private async Task<Guid> CreateJobAsync() =>
        await WithRepositoryAsync(async repository =>
        {
            ProblemValidationJob job = new(Guid.NewGuid());
            await repository.AddAsync(job);
            return job.Id;
        });

    [Fact]
    public async Task IsCancelled_SeesACancelCommittedByAnotherScope()
    {
        Guid jobId = await CreateJobAsync();

        await using AsyncServiceScope workerScope = environment.Factory.Services.CreateAsyncScope();
        var workerRepository = workerScope.ServiceProvider.GetRequiredService<IProblemValidationJobRepository>();

        // The worker loads and mutates the job first, exactly as a validation pass does, so the
        // scope's change tracker holds an instance whose Status predates the cancel below.
        ProblemValidationJob? tracked = await workerRepository.FindByIdAsync(jobId);
        tracked!.BeginStep(ProblemValidationStepKind.ReferenceSolutions);

        Assert.False(await workerRepository.IsCancelledAsync(jobId));

        await WithRepositoryAsync(async repository =>
        {
            ProblemValidationJob? job = await repository.FindByIdAsync(jobId);
            job!.Cancel();
            await repository.UpdateAsync(job);
            return true;
        });

        Assert.True(await workerRepository.IsCancelledAsync(jobId));
    }

    [Fact]
    public async Task IsCancelled_ReturnsNullForAJobThatNoLongerExists()
    {
        Assert.Null(await WithRepositoryAsync(repository => repository.IsCancelledAsync(Guid.NewGuid())));
    }

    [Fact]
    public async Task IsCancelled_IsFalseForAJobStillRunning()
    {
        Guid jobId = await CreateJobAsync();

        Assert.False(await WithRepositoryAsync(repository => repository.IsCancelledAsync(jobId)));
    }
}