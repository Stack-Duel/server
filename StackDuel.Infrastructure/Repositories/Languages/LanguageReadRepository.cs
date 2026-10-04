using Microsoft.EntityFrameworkCore;
using StackDuel.Application.Languages;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.Enums;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Languages;

internal sealed class LanguageReadRepository(StackDuelDbContext context) : ILanguageReadRepository
{
    public async Task<IEnumerable<Language>> FindLanguagesByVersionId(
        IEnumerable<Guid> versionIds,
        CancellationToken cancellationToken
    ) =>
        await context
            .Languages.AsNoTracking()
            .Include(l => l.Versions)
            .Where(l => l.Versions.Any(v => versionIds.Contains(v.Id)))
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Language>> GetActiveLanguagesAsync(CancellationToken cancellationToken) =>
        await context
            .Languages.AsNoTracking()
            .Include(l => l.Versions)
            .Where(l => l.Status == LanguageStatus.Active)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Language>> GetActiveLanguagesByTrackIdsAsync(
        IEnumerable<Guid> trackIds,
        CancellationToken cancellationToken
    )
    {
        List<Guid> trackIdList = trackIds.ToList();

        return await context
            .Languages.AsNoTracking()
            .Include(l => l.Versions)
            .Where(l => l.Status == LanguageStatus.Active && trackIdList.Contains(l.TrackId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetLanguageVersionIdsByLanguageIdsAsync(
        IEnumerable<Guid> languageIds,
        CancellationToken cancellationToken
    )
    {
        List<Guid> languageIdList = languageIds.ToList();

        return await context
            .Languages.AsNoTracking()
            .Where(l => languageIdList.Contains(l.Id))
            .SelectMany(l => l.Versions)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);
    }
}