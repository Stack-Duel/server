using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class DemoDataSeeder(StackDuelDbContext context, Judge0PipelineSeeder pipelineSeeder) : IDemoSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Problems.AnyAsync(cancellationToken))
            return;

        LanguageVersionEntry jsVersion = await context
            .Languages.Where(l => l.Slug == new LanguageSlug("javascript"))
            .SelectMany(l => l.Versions)
            .FirstAsync(cancellationToken);

        LanguageVersionEntry tsVersion = await context
            .Languages.Where(l => l.Slug == new LanguageSlug("typescript"))
            .SelectMany(l => l.Versions)
            .FirstAsync(cancellationToken);

        LanguageVersionEntry pyVersion = await context
            .Languages.Where(l => l.Slug == new LanguageSlug("python"))
            .SelectMany(l => l.Versions)
            .FirstAsync(cancellationToken);

        Problem twoSum = new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(
                "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target. You may assume that each input would have exactly one solution, and you may not use the same element twice."
            ),
            new Difficulty(500),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

        twoSum.Publish();

        Guid pipelineId = await pipelineSeeder.GetOrCreateAsync(cancellationToken);

        ProblemSetup jsSetup = twoSum.AddSetup(
            jsVersion.Id,
            "function twoSum(nums, target) {\n    \n}",
            "twoSum",
            pipelineId
        );

        ProblemSetup tsSetup = twoSum.AddSetup(
            tsVersion.Id,
            "function twoSum(nums: number[], target: number): number[] {\n    \n}",
            "twoSum",
            pipelineId
        );

        ProblemSetup pySetup = twoSum.AddSetup(
            pyVersion.Id,
            "def two_sum(nums: list[int], target: int) -> list[int]:\n    pass",
            "two_sum",
            pipelineId
        );

        context.Problems.Add(twoSum);
        await context.SaveChangesAsync(cancellationToken);

        TestSuite sampleSuite = BuildTwoSumSampleSuite();
        TestSuite hiddenSuite = BuildTwoSumHiddenSuite();

        context.TestSuites.AddRange(sampleSuite, hiddenSuite);
        await context.SaveChangesAsync(cancellationToken);

        Guid[] setupIds = [jsSetup.Id, tsSetup.Id, pySetup.Id];
        foreach (Guid id in setupIds)
        {
            ProblemSetup setup = await context
                .Set<ProblemSetup>()
                .Include(s => s.TestSuites)
                .FirstAsync(s => s.Id == id, cancellationToken);

            ((List<TestSuite>)setup.TestSuites).Add(sampleSuite);
            ((List<TestSuite>)setup.TestSuites).Add(hiddenSuite);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static TestSuite BuildTwoSumSampleSuite()
    {
        TestSuite suite = new("Two Sum - Sample Cases", TestSuiteType.Sample);

        TestCase case1 = suite.AddTestCase("Example 1");
        case1.AddInput("[2,7,11,15]", "integer_array");
        case1.AddInput("9", "integer");
        case1.AddExpectedOutput("[0,1]", "integer_array");

        TestCase case2 = suite.AddTestCase("Example 2");
        case2.AddInput("[3,2,4]", "integer_array");
        case2.AddInput("6", "integer");
        case2.AddExpectedOutput("[1,2]", "integer_array");

        return suite;
    }

    private static TestSuite BuildTwoSumHiddenSuite()
    {
        TestSuite suite = new("Two Sum - Hidden Cases", TestSuiteType.Hidden);

        TestCase case1 = suite.AddTestCase("Hidden 1");
        case1.AddInput("[1,2,3,4,5]", "integer_array");
        case1.AddInput("9", "integer");
        case1.AddExpectedOutput("[3,4]", "integer_array");

        TestCase case2 = suite.AddTestCase("Hidden 2");
        case2.AddInput("[0,4,3,0]", "integer_array");
        case2.AddInput("0", "integer");
        case2.AddExpectedOutput("[0,3]", "integer_array");

        return suite;
    }
}