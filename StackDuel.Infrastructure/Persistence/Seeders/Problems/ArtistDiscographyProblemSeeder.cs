using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

internal sealed class ArtistDiscographyProblemSeeder(StackDuelDbContext context, Judge0PipelineSeeder pipelineSeeder)
    : IStaticSeeder
{
    private const string ProblemSlug = "artist-discography";
    private const string FunctionName = "ArtistList";

    private static readonly string[] Tags = ["react", "state", "lists", "components"];

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
            const { RecordItem } = require("./RecordItem");

            function ArtistList({ artists }) {
              return null;
            }
            """;

        (Guid versionId, string code, string funcName)[] desiredSetups = [(reactVersion.Id, starterCode, FunctionName)];

        ProblemSetupFile[] additionalFiles =
        [
            new(
                "RecordItem.jsx",
                """
                function RecordItem({ record }) {
                  return (
                    <li data-testid={`record-${record.id}`}>
                      {record.title} ({record.year})
                    </li>
                  );
                }

                module.exports.RecordItem = RecordItem;
                """
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
                new Title("Artist Discography"),
                new Question(
                    """
                    Implement a React component named `ArtistList` that receives an `artists` prop — an array of `{ id, name, records }` objects, where `records` is an array of `{ id, title, year }` objects — and renders each artist with their records collapsed by default, expandable independently on click.

                    ### Requirements

                    - Render a `<div data-testid="artist-list">` wrapping one entry per artist.
                    - Each artist entry needs a `<button data-testid="toggle-{artist.id}">` showing the artist's name, which toggles that artist's records visibility on click.
                    - Records must start collapsed (not rendered at all) and only appear once toggled.
                    - When expanded, render `<ul data-testid="records-{artist.id}">` containing one `<li data-testid="record-{record.id}">` per record with the text `{title} ({year})`.
                    - Each artist's expanded/collapsed state is independent of the others — expanding one must not affect any other artist.
                    - An artist with zero records still renders an (empty) `<ul>` once expanded.

                    ### Example 1

                    ```
                    Input: artists = [{ id: "a1", name: "Radiohead", records: [{ id: "r1", title: "OK Computer", year: 1997 }] }]
                    Render (collapsed): <button data-testid="toggle-a1">Radiohead</button>
                    ```

                    ### Example 2

                    ```
                    Input: artists = [{ id: "a1", name: "Radiohead", records: [{ id: "r1", title: "OK Computer", year: 1997 }] }]
                    After clicking "Radiohead": adds <ul data-testid="records-a1"><li data-testid="record-r1">OK Computer (1997)</li></ul>
                    ```
                    """
                ),
                new Difficulty(600),
                new TimeLimit(2000),
                new MemoryLimit(64)
            );

            foreach ((Guid versionId, string code, string funcName) in desiredSetups)
            {
                ProblemSetup setup = problem.AddSetup(versionId, code, funcName, pipelineId);
                setup.SetAdditionalFiles(additionalFiles);
            }

            problem.Archive();

            context.Problems.Add(problem);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return problem.Id;
        }

        Guid problemId = existingId.Value;

        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE problems SET status = 3 WHERE id = {problemId} AND status <> 3
            """,
            cancellationToken
        );

        HashSet<Guid> existingVersionIds = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(s => EF.Property<Guid>(s, "problem_id") == problemId)
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
                    INSERT INTO problem_setups (id, problem_id, language_version_id, initial_code, function_name, pipeline_id, additional_files)
                    VALUES ({Guid.NewGuid()}, {problemId}, {versionId}, {code}, {funcName}, {pipelineId}, {System.Text.Json.JsonSerializer.Serialize(
                        additionalFiles,
                        (System.Text.Json.JsonSerializerOptions?)null
                    )})
                    """,
                    cancellationToken
                );
            }
        }

        ProblemSetup? reactSetup = await context
            .Set<ProblemSetup>()
            .Where(s => EF.Property<Guid>(s, "problem_id") == problemId && s.LanguageVersionId == reactVersion.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (reactSetup is not null)
        {
            reactSetup.UpdateInitialCode(starterCode);
            reactSetup.SetAdditionalFiles(additionalFiles);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
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
            .Languages.IgnoreQueryFilters()
            .Where(l => l.Slug == new LanguageSlug(slug))
            .SelectMany(l => l.Versions)
            .FirstAsync(cancellationToken);

    private const string TwoArtistsProps =
        """{"artists":[{"id":"a1","name":"Radiohead","records":[{"id":"r1","title":"OK Computer","year":1997},{"id":"r2","title":"Kid A","year":2000}]},{"id":"a2","name":"Portishead","records":[{"id":"r3","title":"Dummy","year":1994}]}]}""";

    private const string ThreeArtistsProps =
        """{"artists":[{"id":"b1","name":"Fontaines D.C.","records":[{"id":"r1","title":"Dogrel","year":2019}]},{"id":"b2","name":"black midi","records":[]},{"id":"b3","name":"Black Country, New Road","records":[{"id":"r2","title":"Ants From Up There","year":2022},{"id":"r3","title":"For the first time","year":2021}]}]}""";

    // Expected outputs are the exact stdout produced by the harness (a normalized JSON
    // render-tree) for a correct reference solution — generated by actually running the
    // real strategy + harness rather than hand-typed, since the grading comparison
    // (SetEquality) reformats whitespace but does not reorder JSON object keys.
    private static TestSuite BuildSampleSuite()
    {
        TestSuite suite = new("Artist Discography - Sample Cases", TestSuiteType.Sample);

        TestCase case1 = suite.AddTestCase("Collapsed by default");
        case1.AddInput(TwoArtistsProps, "props");
        case1.AddInput("[]", "actions");
        case1.AddExpectedOutput(
            """{"type":"div","props":{"data-testid":"artist-list"},"children":[{"type":"div","props":{"data-testid":"artist-a1"},"children":[{"type":"button","props":{"data-testid":"toggle-a1"},"children":["Radiohead"]}]},{"type":"div","props":{"data-testid":"artist-a2"},"children":[{"type":"button","props":{"data-testid":"toggle-a2"},"children":["Portishead"]}]}]}""",
            "json"
        );

        TestCase case2 = suite.AddTestCase("Expands on click");
        case2.AddInput(TwoArtistsProps, "props");
        case2.AddInput("""[{"testId":"toggle-a1","type":"click"}]""", "actions");
        case2.AddExpectedOutput(
            """{"type":"div","props":{"data-testid":"artist-list"},"children":[{"type":"div","props":{"data-testid":"artist-a1"},"children":[{"type":"button","props":{"data-testid":"toggle-a1"},"children":["Radiohead"]},{"type":"ul","props":{"data-testid":"records-a1"},"children":[{"type":"li","props":{"data-testid":"record-r1"},"children":["OK Computer"," (","1997",")"]},{"type":"li","props":{"data-testid":"record-r2"},"children":["Kid A"," (","2000",")"]}]}]},{"type":"div","props":{"data-testid":"artist-a2"},"children":[{"type":"button","props":{"data-testid":"toggle-a2"},"children":["Portishead"]}]}]}""",
            "json"
        );

        return suite;
    }

    private static TestSuite BuildHiddenSuite()
    {
        TestSuite suite = new("Artist Discography - Hidden Cases", TestSuiteType.Hidden);

        TestCase case1 = suite.AddTestCase("Toggle twice collapses again");
        case1.AddInput(TwoArtistsProps, "props");
        case1.AddInput("""[{"testId":"toggle-a1","type":"click"},{"testId":"toggle-a1","type":"click"}]""", "actions");
        case1.AddExpectedOutput(
            """{"type":"div","props":{"data-testid":"artist-list"},"children":[{"type":"div","props":{"data-testid":"artist-a1"},"children":[{"type":"button","props":{"data-testid":"toggle-a1"},"children":["Radiohead"]}]},{"type":"div","props":{"data-testid":"artist-a2"},"children":[{"type":"button","props":{"data-testid":"toggle-a2"},"children":["Portishead"]}]}]}""",
            "json"
        );

        TestCase case2 = suite.AddTestCase("Independent expansion and empty records");
        case2.AddInput(ThreeArtistsProps, "props");
        case2.AddInput(
            """[{"testId":"toggle-b1","type":"click"},{"testId":"toggle-b2","type":"click"},{"testId":"toggle-b3","type":"click"}]""",
            "actions"
        );
        case2.AddExpectedOutput(
            """{"type":"div","props":{"data-testid":"artist-list"},"children":[{"type":"div","props":{"data-testid":"artist-b1"},"children":[{"type":"button","props":{"data-testid":"toggle-b1"},"children":["Fontaines D.C."]},{"type":"ul","props":{"data-testid":"records-b1"},"children":[{"type":"li","props":{"data-testid":"record-r1"},"children":["Dogrel"," (","2019",")"]}]}]},{"type":"div","props":{"data-testid":"artist-b2"},"children":[{"type":"button","props":{"data-testid":"toggle-b2"},"children":["black midi"]},{"type":"ul","props":{"data-testid":"records-b2"},"children":[]}]},{"type":"div","props":{"data-testid":"artist-b3"},"children":[{"type":"button","props":{"data-testid":"toggle-b3"},"children":["Black Country, New Road"]},{"type":"ul","props":{"data-testid":"records-b3"},"children":[{"type":"li","props":{"data-testid":"record-r2"},"children":["Ants From Up There"," (","2022",")"]},{"type":"li","props":{"data-testid":"record-r3"},"children":["For the first time"," (","2021",")"]}]}]}]}""",
            "json"
        );

        return suite;
    }
}