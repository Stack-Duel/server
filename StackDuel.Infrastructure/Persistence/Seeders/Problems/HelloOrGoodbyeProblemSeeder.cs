using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

internal sealed class HelloOrGoodbyeProblemSeeder(
    StackDuelDbContext context,
    Judge0PipelineSeeder pipelineSeeder,
    ITestCaseGenerationJobRepository testCaseGenerationJobRepository
) : IStaticSeeder
{
    private const string ProblemSlug = "hello-or-goodbye";

    private static readonly string[] Tags = ["math", "conditionals"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Guid problemId = await EnsureProblemWithSetupsAsync(cancellationToken);
        await EnsureTestSuitesLinkedAsync(problemId, cancellationToken);
        await EnsureTagsLinkedAsync(problemId, cancellationToken);

        context.ChangeTracker.Clear();
        await EnsureGeneratedTestCasesAsync(problemId, cancellationToken);
    }

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
            def hello_or_goodbye(n: int) -> str:
                return "Hello" if n % 2 == 0 else "Goodbye"
            """;

        GenerationParameterSpec[] parameters =
        [
            new("n", "integer", Min: -1000, Max: 1000, LengthMin: null, LengthMax: null, Charset: null),
        ];

        await TestCaseGenerationSeederHelper.EnqueueIfNeededAsync(
            context,
            testCaseGenerationJobRepository,
            "Hello or Goodbye",
            setupInfo.Id,
            setupInfo.GenerationSpecId,
            referenceSolution,
            parameters,
            "string",
            targetCaseCount: 20,
            seed: 11111,
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
            (jsVersion.Id, "function helloOrGoodbye(n) {\n    \n}", "helloOrGoodbye"),
            (pyVersion.Id, "def hello_or_goodbye(n: int) -> str:\n    pass", "hello_or_goodbye"),
            (
                javaVersion.Id,
                "class Solution {\n    public String helloOrGoodbye(int n) {\n        return null;\n    }\n}",
                "helloOrGoodbye"
            ),
            (
                cppVersion.Id,
                "class Solution {\npublic:\n    string helloOrGoodbye(int n) {\n        return \"\";\n    }\n};",
                "helloOrGoodbye"
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
                new Title("Hello or Goodbye"),
                new Question(
                    """
                    Given an integer `n`, return `"Hello"` if `n` is even, or `"Goodbye"` if `n` is odd.

                    ### Example 1

                    ```
                    Input: n = 4
                    Output: "Hello"
                    Explanation: 4 is even, so the answer is "Hello".
                    ```

                    ### Example 2

                    ```
                    Input: n = 7
                    Output: "Goodbye"
                    Explanation: 7 is odd, so the answer is "Goodbye".
                    ```

                    ### Example 3

                    ```
                    Input: n = 0
                    Output: "Hello"
                    Explanation: 0 is even, so the answer is "Hello".
                    ```

                    ### Constraints

                    1. `-10^9 <= n <= 10^9`
                    """
                ),
                new Difficulty(100),
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

            List<Guid> setupIds = await context
                .Set<ProblemSetup>()
                .AsNoTracking()
                .Where(s => EF.Property<Guid>(s, "problem_id") == problemId)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            foreach (Guid setupId in setupIds)
            {
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
        TestSuite suite = new("Hello or Goodbye - Sample Cases", TestSuiteType.Sample);

        TestCase case1 = suite.AddTestCase("Even number");
        case1.AddInput("4", "integer");
        case1.AddExpectedOutput("Hello", "string");

        TestCase case2 = suite.AddTestCase("Odd number");
        case2.AddInput("7", "integer");
        case2.AddExpectedOutput("Goodbye", "string");

        TestCase case3 = suite.AddTestCase("Zero");
        case3.AddInput("0", "integer");
        case3.AddExpectedOutput("Hello", "string");

        return suite;
    }

    private static TestSuite BuildHiddenSuite()
    {
        TestSuite suite = new("Hello or Goodbye - Hidden Cases", TestSuiteType.Hidden);

        TestCase case1 = suite.AddTestCase("Large even");
        case1.AddInput("1000000", "integer");
        case1.AddExpectedOutput("Hello", "string");

        TestCase case2 = suite.AddTestCase("Large odd");
        case2.AddInput("999999", "integer");
        case2.AddExpectedOutput("Goodbye", "string");

        TestCase case3 = suite.AddTestCase("Negative even");
        case3.AddInput("-2", "integer");
        case3.AddExpectedOutput("Hello", "string");

        TestCase case4 = suite.AddTestCase("Negative odd");
        case4.AddInput("-3", "integer");
        case4.AddExpectedOutput("Goodbye", "string");

        return suite;
    }
}