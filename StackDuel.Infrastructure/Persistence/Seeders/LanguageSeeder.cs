using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Persistence.Seeders;

internal sealed class LanguageSeeder(StackDuelDbContext context) : IStaticSeeder
{
    private static readonly DesiredLanguage[] DesiredLanguages =
    [
        new(
            "JavaScript",
            "javascript",
            TrackSeeder.GeneralPurposeKey,
            [new(new LanguageVersion("Node.js 22.08.0"), new Judge0Id(102))],
            IsActive: true
        ),
        new(
            "TypeScript",
            "typescript",
            TrackSeeder.GeneralPurposeKey,
            [new(new LanguageVersion("5.6.2"), new Judge0Id(101))],
            IsActive: false
        ),
        new(
            "Python",
            "python",
            TrackSeeder.GeneralPurposeKey,
            [new(new LanguageVersion("3.13.2"), new Judge0Id(109))],
            IsActive: true
        ),
        new(
            "SQLite",
            "sqlite",
            TrackSeeder.SqlKey,
            [new(new LanguageVersion("3.27.2"), new Judge0Id(82))],
            IsActive: true
        ),
        new(
            "Java",
            "java",
            TrackSeeder.GeneralPurposeKey,
            [new(new LanguageVersion("JDK 17.0.6"), new Judge0Id(89))],
            IsActive: true
        ),
        new(
            "C++",
            "cpp",
            TrackSeeder.GeneralPurposeKey,
            [new(new LanguageVersion("(GCC 9.2.0)"), new Judge0Id(54))],
            IsActive: true
        ),
        new(
            "React",
            "react",
            TrackSeeder.FrontendKey,
            [new(new LanguageVersion("19.0.0"), new Judge0Id(89))],
            IsActive: false
        ),
        new(
            "Angular",
            "angular",
            TrackSeeder.FrontendKey,
            [new(new LanguageVersion("18.2.0"), new Judge0Id(89))],
            IsActive: false
        ),
        new(
            "Vanilla JS",
            "vanilla-js",
            TrackSeeder.FrontendKey,
            [new(new LanguageVersion("ES2023"), new Judge0Id(89))],
            IsActive: false
        ),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        List<Language> existingLanguages = await context
            .Languages.IgnoreQueryFilters()
            .Include(l => l.Versions)
            .ToListAsync(cancellationToken);

        Dictionary<string, Guid> trackIdsByKey = await context.Tracks.ToDictionaryAsync(
            t => t.Key,
            t => t.Id,
            cancellationToken
        );

        foreach (DesiredLanguage desired in DesiredLanguages)
        {
            if (!trackIdsByKey.TryGetValue(desired.TrackKey, out Guid trackId))
                throw new InvalidOperationException(
                    $"Track '{desired.TrackKey}' was not found — ensure TrackSeeder runs before LanguageSeeder."
                );

            Language? language = existingLanguages.FirstOrDefault(l => l.Slug == new LanguageSlug(desired.Slug));

            if (language is null)
            {
                language = new Language(new LanguageName(desired.Name), new LanguageSlug(desired.Slug), trackId);
                context.Languages.Add(language);
            }

            if (desired.IsActive && !language.IsActive)
                language.Activate();
            else if (!desired.IsActive && language.IsActive)
                language.Deactivate();

            HashSet<int> desiredJudge0Ids = [.. desired.Versions.Select(v => v.Judge0Id.Value)];

            foreach (LanguageVersionEntry existing in language.Versions)
            {
                if (!desiredJudge0Ids.Contains(existing.Judge0Id.Value) || !desired.IsActive)
                {
                    if (existing.IsActive)
                        existing.Deprecate();
                }
                else if (!existing.IsActive)
                {
                    existing.Activate();
                }
            }

            HashSet<int> existingJudge0Ids = [.. language.Versions.Select(v => v.Judge0Id.Value)];

            foreach (DesiredVersion version in desired.Versions)
            {
                if (!existingJudge0Ids.Contains(version.Judge0Id.Value))
                {
                    LanguageVersionEntry entry = language.AddVersion(version.Version, version.Judge0Id);
                    context.Entry(entry).State = EntityState.Added;

                    if (!desired.IsActive)
                        entry.Deprecate();
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record DesiredLanguage(
        string Name,
        string Slug,
        string TrackKey,
        DesiredVersion[] Versions,
        bool IsActive
    );

    private sealed record DesiredVersion(LanguageVersion Version, Judge0Id Judge0Id);
}