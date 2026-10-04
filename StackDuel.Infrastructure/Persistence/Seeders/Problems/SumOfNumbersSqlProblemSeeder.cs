using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

internal sealed class SumOfNumbersSqlProblemSeeder(StackDuelDbContext context, Judge0PipelineSeeder pipelineSeeder)
    : IStaticSeeder
{
    private const string ProblemSlug = "sum-of-numbers-sql";
    private const string ProblemTitle = "Sum of Numbers";

    private static readonly string[] Tags = ["sql", "aggregation"];

    private const string Question = """
        Write a query that returns the sum of all values in the `numbers` table.

        | Column | Type    | Description         |
        |--------|---------|----------------------|
        | value  | INTEGER | One number per row   |

        ### Example 1

        ```
        numbers table:
         value
        -------
             1
             2
             3

        Output: 6
        ```

        ### Example 2

        ```
        numbers table:
         value
        -------
            10
            -4
             5

        Output: 11
        ```
        """;

    private const string InitialCode = """
        -- Write your SQLite query statement below

        """;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Guid problemId = await EnsureProblemWithSetupsAsync(cancellationToken);
        await EnsureTestSuitesLinkedAsync(problemId, cancellationToken);
        await EnsureTagsLinkedAsync(problemId, cancellationToken);
    }

    private async Task<Guid> EnsureProblemWithSetupsAsync(CancellationToken cancellationToken)
    {
        Guid pipelineId = await pipelineSeeder.GetOrCreateAsync(cancellationToken);

        LanguageVersionEntry sqliteVersion = await GetVersionAsync("sqlite", cancellationToken);

        context.ChangeTracker.Clear();

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
                new Title(ProblemTitle),
                new Question(Question),
                new Difficulty(150),
                new TimeLimit(1000),
                new MemoryLimit(64)
            );

            problem.Publish();
            problem.AddSetup(sqliteVersion.Id, InitialCode, null, pipelineId);

            context.Problems.Add(problem);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return problem.Id;
        }

        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE problems SET title = {ProblemTitle}, question = {Question} WHERE id = {existingId.Value}
            """,
            cancellationToken
        );

        Guid? setupId = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(s =>
                EF.Property<Guid>(s, "problem_id") == existingId.Value && s.LanguageVersionId == sqliteVersion.Id
            )
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (setupId is null)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO problem_setups (id, problem_id, language_version_id, initial_code, function_name, pipeline_id)
                VALUES ({Guid.NewGuid()}, {existingId.Value}, {sqliteVersion.Id}, {InitialCode}, {(string?)
                    null}, {pipelineId})
                """,
                cancellationToken
            );
        }
        else
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                UPDATE problem_setups SET initial_code = {InitialCode} WHERE id = {setupId.Value}
                """,
                cancellationToken
            );
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
        TestSuite suite = new("Sum of Numbers - Sample Cases", TestSuiteType.Sample);

        TestCase case1 = suite.AddTestCase("Three positive values");
        case1.AddInput(
            "CREATE TABLE numbers(value INTEGER);\nINSERT INTO numbers(value) VALUES (1),(2),(3);",
            "sql_setup"
        );
        case1.AddExpectedOutput("6", "integer");

        TestCase case2 = suite.AddTestCase("Includes a negative value");
        case2.AddInput(
            "CREATE TABLE numbers(value INTEGER);\nINSERT INTO numbers(value) VALUES (10),(-4),(5);",
            "sql_setup"
        );
        case2.AddExpectedOutput("11", "integer");

        return suite;
    }

    private static TestSuite BuildHiddenSuite()
    {
        TestSuite suite = new("Sum of Numbers - Hidden Cases", TestSuiteType.Hidden);

        TestCase case1 = suite.AddTestCase("Single row");
        case1.AddInput("CREATE TABLE numbers(value INTEGER);\nINSERT INTO numbers(value) VALUES (42);", "sql_setup");
        case1.AddExpectedOutput("42", "integer");

        TestCase case2 = suite.AddTestCase("Larger set");
        case2.AddInput(
            "CREATE TABLE numbers(value INTEGER);\nINSERT INTO numbers(value) VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10);",
            "sql_setup"
        );
        case2.AddExpectedOutput("55", "integer");

        return suite;
    }
}