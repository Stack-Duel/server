using Microsoft.Extensions.DependencyInjection;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public sealed class ProblemVersionPersistenceTests(IntegrationTestEnvironment environment)
{
    private async Task<T> WithRepositoryAsync<T>(Func<IProblemRepository, Task<T>> work)
    {
        await using AsyncServiceScope scope = environment.Factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProblemRepository>();

        return await work(repository);
    }

    private static Problem CreateProblem(string slug) =>
        new(
            new Slug(slug),
            new Title("Count Vowels"),
            new Question(new string('q', 60)),
            new Difficulty(300),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    private async Task<Guid> CreatePublishedProblemAsync()
    {
        string slug = $"version-persist-{Guid.NewGuid():N}";

        return await WithRepositoryAsync(async repository =>
        {
            Problem problem = CreateProblem(slug);
            problem.CompleteValidation();

            await repository.AddAsync(problem);

            return problem.Id;
        });
    }

    [Fact]
    public async Task FindById_LoadsTheVersionsSoRepublishingDoesNotCollide()
    {
        Guid problemId = await CreatePublishedProblemAsync();

        Problem? reloaded = await WithRepositoryAsync(repository => repository.FindByIdAsync(problemId));

        Assert.NotNull(reloaded);
        Assert.Single(reloaded.Versions);
        Assert.NotNull(reloaded.PublishedVersion);
    }

    [Fact]
    public async Task Republishing_AddsASecondVersionAndSupersedesTheFirst()
    {
        Guid problemId = await CreatePublishedProblemAsync();

        await WithRepositoryAsync(async repository =>
        {
            Problem? problem = await repository.FindByIdAsync(problemId);

            problem!.UpdateContent(
                new Title("Count Vowels 2"),
                problem.Question,
                problem.Difficulty,
                problem.TimeLimit,
                problem.MemoryLimit
            );
            problem.SubmitForValidation();
            problem.CompleteValidation();

            await repository.UpdateAsync(problem);

            return true;
        });

        Problem? reloaded = await WithRepositoryAsync(repository => repository.FindByIdAsync(problemId));

        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded.Versions.Count);
        Assert.Equal("Count Vowels 2", reloaded.PublishedVersion!.Title.Value);
        Assert.Equal(2, reloaded.PublishedVersion!.VersionNumber);
        Assert.Equal(
            ProblemVersionState.Superseded,
            reloaded.Versions.Single(version => version.VersionNumber == 1).State
        );
        Assert.False(reloaded.HasUnpublishedChanges);
        Assert.False(reloaded.IsRevalidating);
    }
}
