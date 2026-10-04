using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;

namespace StackDuel.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the initial set of languages every new problem must ship a reference solution for.
/// Admin-editable afterwards via the required-languages settings page — this only sets the
/// starting point on a fresh database.
/// </summary>
internal sealed class RequiredProblemLanguageSeeder(StackDuelDbContext context) : IStaticSeeder
{
    private static readonly string[] DesiredSlugs = ["python", "javascript"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.RequiredProblemLanguages.AnyAsync(cancellationToken))
            return;

        int sortOrder = 0;
        foreach (string slug in DesiredSlugs)
        {
            LanguageVersionEntry? version = await context
                .Languages.Where(l => l.Slug == new LanguageSlug(slug))
                .SelectMany(l => l.Versions)
                .FirstOrDefaultAsync(cancellationToken);

            if (version is null)
                continue;

            context.RequiredProblemLanguages.Add(RequiredProblemLanguage.Create(version.Id, sortOrder));
            sortOrder++;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}