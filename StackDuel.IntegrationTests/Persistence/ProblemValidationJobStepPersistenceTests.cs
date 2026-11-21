using Microsoft.Extensions.DependencyInjection;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProblemValidationJobStepPersistenceTests(IntegrationTestEnvironment environment)
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
    public async Task FindById_LoadsTheStepsSoTheWorkerCanRecordProgress()
    {
        Guid jobId = await CreateJobAsync();

        ProblemValidationJob? reloaded = await WithRepositoryAsync(repository => repository.FindByIdAsync(jobId));

        Assert.NotNull(reloaded);
        Assert.Equal(Enum.GetValues<ProblemValidationStepKind>().Length, reloaded.Steps.Count);
    }

    [Fact]
    public async Task StepProgress_SurvivesASaveAndReload()
    {
        Guid jobId = await CreateJobAsync();

        await WithRepositoryAsync(async repository =>
        {
            ProblemValidationJob? job = await repository.FindByIdAsync(jobId);

            job!.SucceedStep(ProblemValidationStepKind.AuthorChecks, "2 setup(s) present.");
            job.BeginStep(ProblemValidationStepKind.ReferenceSolutions);
            job.FailStep(
                ProblemValidationStepKind.ReferenceSolutions,
                "Case 'Y counts when asked': runtime error.",
                [
                    new ProblemValidationStepItemSpec(
                        "JavaScript",
                        ProblemValidationStepStatus.Failed,
                        "SyntaxError: Unexpected token 'const'"
                    ),
                    new ProblemValidationStepItemSpec("Python", ProblemValidationStepStatus.Succeeded),
                ]
            );
            job.Fail("Case 'Y counts when asked': runtime error.");

            await repository.UpdateAsync(job);

            return true;
        });

        ProblemValidationJob? reloaded = await WithRepositoryAsync(repository => repository.FindByIdAsync(jobId));

        Assert.NotNull(reloaded);
        Assert.Equal(ProblemValidationJobStatus.Failed, reloaded.Status);

        ProblemValidationStep authorChecks = reloaded.Steps.Single(step =>
            step.Kind == ProblemValidationStepKind.AuthorChecks
        );
        ProblemValidationStep referenceSolutions = reloaded.Steps.Single(step =>
            step.Kind == ProblemValidationStepKind.ReferenceSolutions
        );
        ProblemValidationStep generation = reloaded.Steps.Single(step =>
            step.Kind == ProblemValidationStepKind.TestCaseGeneration
        );

        Assert.Equal(ProblemValidationStepStatus.Succeeded, authorChecks.Status);
        Assert.Equal("2 setup(s) present.", authorChecks.Detail);

        Assert.Equal(ProblemValidationStepStatus.Failed, referenceSolutions.Status);
        Assert.Equal("Case 'Y counts when asked': runtime error.", referenceSolutions.Detail);
        Assert.Equal(2, referenceSolutions.Items.Count);
        Assert.Contains(referenceSolutions.Items, item =>
            item.Label == "JavaScript" && item.Detail == "SyntaxError: Unexpected token 'const'"
        );

        Assert.Equal(ProblemValidationStepStatus.Skipped, generation.Status);
    }
}