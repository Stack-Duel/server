using StackDuel.Application.Problems.RequiredLanguages;
using StackDuel.Application.Problems.RequiredLanguages.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;
using StackDuel.Domain.Tracks.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class RequiredProblemLanguageReadRepository(StackDuelDbContext context)
    : IRequiredProblemLanguageReadRepository
{
    public async Task<IReadOnlyList<RequiredProblemLanguageAdminDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        List<RequiredProblemLanguage> required = await context
            .RequiredProblemLanguages.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (required.Count == 0)
            return [];

        List<Language> languages = await context.Languages.AsNoTracking().ToListAsync(cancellationToken);

        List<Track> tracks = await context.Tracks.AsNoTracking().ToListAsync(cancellationToken);

        List<RequiredProblemLanguageAdminDto> result = [];
        foreach (RequiredProblemLanguage entry in required)
        {
            Language? language = languages.FirstOrDefault(l => l.Versions.Any(v => v.Id == entry.LanguageVersionId));
            LanguageVersionEntry? version = language?.Versions.FirstOrDefault(v => v.Id == entry.LanguageVersionId);
            Track? track = language is not null ? tracks.FirstOrDefault(t => t.Id == language.TrackId) : null;

            result.Add(
                new RequiredProblemLanguageAdminDto(
                    entry.Id,
                    entry.LanguageVersionId,
                    language?.Name.Value ?? "Unknown language",
                    version?.Version.Value ?? "Unknown version",
                    entry.SortOrder,
                    track?.Id,
                    track?.Key,
                    track?.Name
                )
            );
        }

        return result;
    }
}