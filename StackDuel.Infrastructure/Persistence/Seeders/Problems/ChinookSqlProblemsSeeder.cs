using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace StackDuel.Infrastructure.Persistence.Seeders.Problems;

internal sealed class ChinookSqlProblemsSeeder(
    StackDuelDbContext context,
    Judge0PipelineSeeder pipelineSeeder,
    AdditionalFileBundleSeeder additionalFileBundleSeeder
) : IStaticSeeder
{
    private const string BundleName = "chinook-sqlite-db";
    private const string ResourceName = "StackDuel.Infrastructure.Persistence.Seeders.Assets.chinook-sqlite.zip";

    private const string ArtistsTableDescription = """
        | Column   | Type    | Description             |
        |----------|---------|--------------------------|
        | ArtistId | INTEGER | Primary key              |
        | Name     | TEXT    | Artist name (nullable)   |
        """;

    private const string InitialCode = """
        -- Write your SQLite query statement below

        """;

    private static readonly ProblemSeedDefinition[] ProblemSeeds =
    [
        new(
            Slug: "artists-starting-with-a-sql",
            Title: "Artists Starting With A",
            Question: $"""
            Write a query that returns the names of all artists whose name starts with 'A', ordered by `ArtistId`.

            The `Artists` table:

            {ArtistsTableDescription}
            """,
            Difficulty: 200,
            Tags: ["sql", "filtering"],
            ExpectedOutput: """
            AC/DC
            Accept
            Aerosmith
            Alanis Morissette
            Alice In Chains
            Antônio Carlos Jobim
            Apocalyptica
            Audioslave
            Azymuth
            A Cor Do Som
            Aquaman
            Aerosmith & Sierra Leone's Refugee Allstars
            Avril Lavigne
            Aisha Duo
            Aaron Goldberg
            Alberto Turco & Nova Schola Gregoriana
            Anne-Sophie Mutter, Herbert Von Karajan & Wiener Philharmoniker
            Academy of St. Martin in the Fields & Sir Neville Marriner
            Academy of St. Martin in the Fields Chamber Ensemble & Sir Neville Marriner
            Academy of St. Martin in the Fields, John Birch, Sir Neville Marriner & Sylvia McNair
            Aaron Copland & London Symphony Orchestra
            Academy of St. Martin in the Fields, Sir Neville Marriner & William Bennett
            Antal Doráti & London Symphony Orchestra
            Amy Winehouse
            Academy of St. Martin in the Fields, Sir Neville Marriner & Thurston Dart
            Adrian Leaper & Doreen de Feis
            """
        ),
        new(
            Slug: "count-all-artists-sql",
            Title: "Count All Artists",
            Question: $"""
            Write a query that returns the total number of artists in the table.

            The `Artists` table:

            {ArtistsTableDescription}
            """,
            Difficulty: 100,
            Tags: ["sql", "aggregation"],
            ExpectedOutput: "275"
        ),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        byte[] bundleContent = ReadEmbeddedBundle();
        Guid bundleId = await additionalFileBundleSeeder.GetOrCreateAsync(BundleName, bundleContent, cancellationToken);
        Guid pipelineId = await pipelineSeeder.GetOrCreateAsync(cancellationToken);

        LanguageVersionEntry sqliteVersion = await GetVersionAsync("sqlite", cancellationToken);

        context.ChangeTracker.Clear();

        foreach (ProblemSeedDefinition seed in ProblemSeeds)
        {
            Guid problemId = await EnsureProblemWithSetupAsync(
                seed,
                sqliteVersion.Id,
                bundleId,
                pipelineId,
                cancellationToken
            );
            await EnsureTestSuiteLinkedAsync(seed, problemId, cancellationToken);
            await EnsureTagsLinkedAsync(seed, problemId, cancellationToken);
        }
    }

    private static byte[] ReadEmbeddedBundle()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private async Task<Guid> EnsureProblemWithSetupAsync(
        ProblemSeedDefinition seed,
        Guid sqliteVersionId,
        Guid bundleId,
        Guid pipelineId,
        CancellationToken cancellationToken
    )
    {
        Guid? existingId = await context
            .Problems.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.Slug.Value == seed.Slug)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId is null)
        {
            Problem problem = new(
                new Slug(seed.Slug),
                new Title(seed.Title),
                new Question(seed.Question),
                new Difficulty(seed.Difficulty),
                new TimeLimit(1000),
                new MemoryLimit(64)
            );

            problem.Publish();

            ProblemSetup setup = problem.AddSetup(sqliteVersionId, InitialCode, null, pipelineId);
            problem.SetAdditionalFileBundle(setup.Id, bundleId);

            context.Problems.Add(problem);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return problem.Id;
        }

        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE problems SET title = {seed.Title}, question = {seed.Question} WHERE id = {existingId.Value}
            """,
            cancellationToken
        );

        Guid? setupId = await context
            .Set<ProblemSetup>()
            .AsNoTracking()
            .Where(s =>
                EF.Property<Guid>(s, "problem_id") == existingId.Value && s.LanguageVersionId == sqliteVersionId
            )
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (setupId is null)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO problem_setups (id, problem_id, language_version_id, initial_code, function_name, pipeline_id, additional_file_bundle_id)
                VALUES ({Guid.NewGuid()}, {existingId.Value}, {sqliteVersionId}, {InitialCode}, {(string?)
                    null}, {pipelineId}, {bundleId})
                """,
                cancellationToken
            );
        }
        else
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                UPDATE problem_setups SET initial_code = {InitialCode}, additional_file_bundle_id = {bundleId} WHERE id = {setupId.Value}
                """,
                cancellationToken
            );
        }

        return existingId.Value;
    }

    private async Task EnsureTestSuiteLinkedAsync(
        ProblemSeedDefinition seed,
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        TestSuite suite = new($"{seed.Slug} - Sample Cases", TestSuiteType.Sample);
        TestCase testCase = suite.AddTestCase("Chinook fixture");
        testCase.AddExpectedOutput(seed.ExpectedOutput, "string");

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

    private async Task EnsureTagsLinkedAsync(
        ProblemSeedDefinition seed,
        Guid problemId,
        CancellationToken cancellationToken
    )
    {
        foreach (string tagName in seed.Tags)
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

    private sealed record ProblemSeedDefinition(
        string Slug,
        string Title,
        string Question,
        int Difficulty,
        string[] Tags,
        string ExpectedOutput
    );
}