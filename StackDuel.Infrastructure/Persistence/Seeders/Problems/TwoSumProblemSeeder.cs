using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

internal sealed class TwoSumProblemSeeder(
    StackDuelDbContext context,
    Judge0PipelineSeeder pipelineSeeder,
    ITestCaseGenerationJobRepository testCaseGenerationJobRepository
) : IStaticSeeder
{
    private const string ProblemSlug = "two-sum";

    private static readonly string[] Tags = ["array", "hash-table"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Guid problemId = await EnsureProblemWithSetupsAsync(cancellationToken);

        await EnsureTestSuitesLinkedAsync(problemId, cancellationToken);

        await EnsureTagsLinkedAsync(problemId, cancellationToken);

        context.ChangeTracker.Clear();
        await EnsureGeneratedTestCasesAsync(problemId, cancellationToken);
    }

    /// <summary>
    /// Worked example of the random-test-case-generation feature: attaches a reference
    /// solution + declarative constraint spec to the Python setup and queues a background job
    /// to generate a verified batch of random hidden cases. The reference solution raises when
    /// no pair sums to the target, or when more than one pair does — Two Sum's contract
    /// guarantees exactly one solution, so an ambiguous random input (multiple valid pairs) is
    /// just as invalid as an unsolvable one: a correct submission that happens to find a
    /// different valid pair than whichever one got recorded as "the" answer would be marked
    /// wrong for no real reason. Both cases are correctly skipped rather than persisted —
    /// expect a meaningful chunk of the requested count to be skipped in the job's result,
    /// that's the sanity check working.
    /// </summary>
    private async Task EnsureGeneratedTestCasesAsync(Guid problemId, CancellationToken cancellationToken)
    {
        LanguageVersionEntry pyVersion = await GetVersionAsync("python", cancellationToken);

        var setupInfo = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(s => EF.Property<Guid>(s, "problem_id") == problemId && s.LanguageVersionId == pyVersion.Id)
            .Select(s => new { s.Id, s.GenerationSpecId })
            .FirstOrDefaultAsync(cancellationToken);

        if (setupInfo is null)
            return;

        const string referenceSolution = """
            def two_sum(nums: list[int], target: int) -> list[int]:
                solutions = []
                for i in range(len(nums)):
                    for j in range(i + 1, len(nums)):
                        if nums[i] + nums[j] == target:
                            solutions.append([i, j])
                if len(solutions) != 1:
                    raise ValueError("no unique solution")
                return solutions[0]
            """;

        GenerationParameterSpec[] parameters =
        [
            new("nums", "integer_array", Min: -10, Max: 10, LengthMin: 2, LengthMax: 6, Charset: null),
            new("target", "integer", Min: -10, Max: 10, LengthMin: null, LengthMax: null, Charset: null),
        ];

        await TestCaseGenerationSeederHelper.EnqueueIfNeededAsync(
            context,
            testCaseGenerationJobRepository,
            "Two Sum",
            setupInfo.Id,
            setupInfo.GenerationSpecId,
            referenceSolution,
            parameters,
            "integer_array",
            targetCaseCount: 20,
            seed: 12345,
            cancellationToken
        );

        context.ChangeTracker.Clear();
    }

    private async Task<Guid> EnsureProblemWithSetupsAsync(CancellationToken cancellationToken)
    {
        Guid pipelineId = await pipelineSeeder.GetOrCreateAsync(cancellationToken);

        LanguageVersionEntry jsVersion = await GetVersionAsync("javascript", cancellationToken);
        LanguageVersionEntry pyVersion = await GetVersionAsync("python", cancellationToken);
        LanguageVersionEntry javaVersion = await GetVersionAsync("java", cancellationToken);
        LanguageVersionEntry cppVersion = await GetVersionAsync("cpp", cancellationToken);

        context.ChangeTracker.Clear();

        (Guid versionId, string code, string funcName)[] desiredSetups =
        [
            (jsVersion.Id, "function twoSum(nums, target) {\n    \n}", "twoSum"),
            (pyVersion.Id, "def two_sum(nums: list[int], target: int) -> list[int]:\n    pass", "two_sum"),
            (
                javaVersion.Id,
                "class Solution {\n    public int[] twoSum(int[] nums, int target) {\n        return null;\n    }\n}",
                "twoSum"
            ),
            (
                cppVersion.Id,
                "class Solution {\npublic:\n    vector<int> twoSum(vector<int>& nums, int target) {\n        return {};\n    }\n};",
                "twoSum"
            ),
        ];

        Guid? existingId = await context
            .Problems.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.Slug.Value == ProblemSlug)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is null)
        {
            Problem problem = new(
                new Slug(ProblemSlug),
                new Title("Two Sum"),
                new Question(
                    """
                    Given an array of integers `nums` and an integer `target`, return indices of the two numbers such that they add up to `target`.

                    You may assume that each input would have exactly one solution, and you may not use the same element twice. You can return the answer in any order.

                    ### Example 1

                    ```
                    Input: nums = [2,7,11,15], target = 9
                    Output: [0,1]
                    Explanation: nums[0] + nums[1] = 2 + 7 = 9, so the answer is [0, 1].
                    ```

                    ### Example 2

                    ```
                    Input: nums = [3,2,4], target = 6
                    Output: [1,2]
                    Explanation: nums[1] + nums[2] = 2 + 4 = 6, so the answer is [1, 2].
                    ```

                    ### Constraints

                    1. `2 <= nums.length <= 10^4`
                    2. `-10^9 <= nums[i] <= 10^9`
                    3. `-10^9 <= target <= 10^9`
                    4. Exactly one valid pair exists.
                    """
                ),
                new Difficulty(500),
                new TimeLimit(1000),
                new MemoryLimit(64)
            );

            problem.Publish();

            foreach ((Guid versionId, string code, string funcName) in desiredSetups)
                problem.AddSetup(versionId, code, funcName, pipelineId);

            context.Problems.Add(problem);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return problem.Id;
        }

        // Problem exists — add only missing setups
        HashSet<Guid> existingVersionIds = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(s => EF.Property<Guid>(s, "problem_id") == existingId.Value)
            .Select(s => s.LanguageVersionId)
            .ToHashSetAsync(cancellationToken);

        List<(Guid versionId, string code, string funcName)> missing = desiredSetups
            .Where(d => !existingVersionIds.Contains(d.versionId))
            .ToList();

        if (missing.Count > 0)
        {
            // Bypass EF change tracking entirely for setup inserts to avoid
            // DbUpdateConcurrencyException caused by PropertyAccessMode.Field
            // collections (_history, _setups) being in an unresolvable state.
            foreach ((Guid versionId, string code, string funcName) in missing)
            {
                await context.Database.ExecuteSqlAsync(
                    $"""
                    INSERT INTO problem_setups (id, problem_id, language_version_id, initial_code, function_name, pipeline_id)
                    VALUES ({Guid.NewGuid()}, {existingId.Value}, {versionId}, {code}, {funcName}, {pipelineId})
                    """,
                    cancellationToken
                );
            }
        }

        return existingId.Value;
    }

    private async Task EnsureTestSuitesLinkedAsync(Guid problemId, CancellationToken cancellationToken)
    {
        TestSuite[] desired = [BuildSampleSuite(), BuildHiddenSuite()];

        foreach (TestSuite suite in desired)
        {
            // Upsert test suite by name
            TestSuite? existing = await context
                .TestSuites.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == suite.Name, cancellationToken);

            Guid suiteId;
            if (existing is null)
            {
                context.TestSuites.Add(suite);
                await context.SaveChangesAsync(cancellationToken);
                context.ChangeTracker.Clear();
                suiteId = suite.Id;
            }
            else
            {
                suiteId = existing.Id;
            }

            // Get all setup IDs for this problem
            List<Guid> setupIds = await context
                .Set<ProblemSetup>()
                .AsNoTracking()
                .Where(s => EF.Property<Guid>(s, "problem_id") == problemId)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            foreach (Guid setupId in setupIds)
            {
                // Insert join row only if not already linked (raw SQL to avoid field-backed collection issues)
                await context.Database.ExecuteSqlAsync(
                    $"""
                    INSERT INTO problem_setup_test_suites (problem_setup_id, test_suite_id)
                    VALUES ({setupId}, {suiteId})
                    ON CONFLICT DO NOTHING
                    """,
                    cancellationToken
                );
            }
        }
    }

    private async Task EnsureTagsLinkedAsync(Guid problemId, CancellationToken cancellationToken)
    {
        foreach (string tagName in Tags)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO tags (id, name)
                VALUES ({Guid.NewGuid()}, {tagName})
                ON CONFLICT (name) DO NOTHING
                """,
                cancellationToken
            );

            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO problem_tags ("ProblemsId", "TagsId")
                SELECT {problemId}, id FROM tags WHERE name = {tagName}
                ON CONFLICT DO NOTHING
                """,
                cancellationToken
            );
        }
    }

    private async Task<LanguageVersionEntry> GetVersionAsync(string slug, CancellationToken cancellationToken) =>
        await context
            .Languages.Where(l => l.Slug == new LanguageSlug(slug))
            .SelectMany(l => l.Versions)
            .FirstAsync(cancellationToken);

    private static TestSuite BuildSampleSuite()
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

    private static TestSuite BuildHiddenSuite()
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