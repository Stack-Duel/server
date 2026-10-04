using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

internal sealed class ClickCounterProblemSeeder(StackDuelDbContext context, Judge0PipelineSeeder pipelineSeeder)
    : IStaticSeeder
{
    private const string ProblemSlug = "click-counter";
    private const string FunctionName = "Counter";

    private static readonly string[] Tags = ["react", "state", "components"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Guid problemId = await EnsureProblemWithSetupsAsync(cancellationToken);
        await EnsureTestSuitesLinkedAsync(problemId, cancellationToken);
        await EnsureTagsLinkedAsync(problemId, cancellationToken);
    }

    private async Task<Guid> EnsureProblemWithSetupsAsync(CancellationToken cancellationToken)
    {
        Guid pipelineId = await pipelineSeeder.GetOrCreateAsync(cancellationToken);

        LanguageVersionEntry reactVersion = await GetVersionAsync("react", cancellationToken);

        context.ChangeTracker.Clear();

        const string starterCode = """
            function Counter({ start }) {
              return null;
            }
            """;

        (Guid versionId, string code, string funcName)[] desiredSetups = [(reactVersion.Id, starterCode, FunctionName)];

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
                new Title("Click Counter"),
                new Question(
                    """
                    Implement a React component named `Counter` that receives a `start` prop and renders a single `<button>` showing the current count.

                    ### Requirements

                    - The button must have the attribute `data-testid="count"`.
                    - The button's text content is the current count, beginning at `start`.
                    - Each click on the button increments the count by 1.

                    ### Example 1

                    ```
                    Input: start = 0
                    Render: <button data-testid="count">0</button>
                    ```

                    ### Example 2

                    ```
                    Input: start = 5
                    After one click: <button data-testid="count">6</button>
                    ```
                    """
                ),
                new Difficulty(200),
                new TimeLimit(2000),
                new MemoryLimit(64)
            );

            foreach ((Guid versionId, string code, string funcName) in desiredSetups)
                problem.AddSetup(versionId, code, funcName, pipelineId);

            problem.Archive();

            context.Problems.Add(problem);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return problem.Id;
        }

        Guid resolvedProblemId = existingId.Value;

        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE problems SET status = 3 WHERE id = {resolvedProblemId} AND status <> 3
            """,
            cancellationToken
        );

        HashSet<Guid> existingVersionIds = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(s => EF.Property<Guid>(s, "problem_id") == resolvedProblemId)
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
                    VALUES ({Guid.NewGuid()}, {resolvedProblemId}, {versionId}, {code}, {funcName}, {pipelineId})
                    """,
                    cancellationToken
                );
            }
        }

        return resolvedProblemId;
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
            .Languages.IgnoreQueryFilters()
            .Where(l => l.Slug == new LanguageSlug(slug))
            .SelectMany(l => l.Versions)
            .FirstAsync(cancellationToken);

    // Expected outputs are the exact stdout produced by the harness (a normalized JSON
    // render-tree) for a correct reference solution — generated by actually running the
    // real strategy + harness rather than hand-typed, since the grading comparison
    // (SetEquality) reformats whitespace but does not reorder JSON object keys.
    private static TestSuite BuildSampleSuite()
    {
        TestSuite suite = new("Click Counter - Sample Cases", TestSuiteType.Sample);

        TestCase case1 = suite.AddTestCase("Initial render");
        case1.AddInput("""{"start":0}""", "props");
        case1.AddInput("[]", "actions");
        case1.AddExpectedOutput("""{"type":"button","props":{"data-testid":"count"},"children":["0"]}""", "json");

        TestCase case2 = suite.AddTestCase("Increments on click");
        case2.AddInput("""{"start":5}""", "props");
        case2.AddInput("""[{"testId":"count","type":"click"}]""", "actions");
        case2.AddExpectedOutput("""{"type":"button","props":{"data-testid":"count"},"children":["6"]}""", "json");

        return suite;
    }

    private static TestSuite BuildHiddenSuite()
    {
        TestSuite suite = new("Click Counter - Hidden Cases", TestSuiteType.Hidden);

        TestCase case1 = suite.AddTestCase("Two clicks");
        case1.AddInput("""{"start":10}""", "props");
        case1.AddInput("""[{"testId":"count","type":"click"},{"testId":"count","type":"click"}]""", "actions");
        case1.AddExpectedOutput("""{"type":"button","props":{"data-testid":"count"},"children":["12"]}""", "json");

        TestCase case2 = suite.AddTestCase("Negative start back to zero");
        case2.AddInput("""{"start":-3}""", "props");
        case2.AddInput(
            """[{"testId":"count","type":"click"},{"testId":"count","type":"click"},{"testId":"count","type":"click"}]""",
            "actions"
        );
        case2.AddExpectedOutput("""{"type":"button","props":{"data-testid":"count"},"children":["0"]}""", "json");

        return suite;
    }
}