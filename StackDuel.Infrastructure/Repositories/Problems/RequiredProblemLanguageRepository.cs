using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Problems.RequiredLanguages;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Problems;

internal sealed class RequiredProblemLanguageRepository(StackDuelDbContext context) : IRequiredProblemLanguageRepository
{
    public async Task AddAsync(RequiredProblemLanguage entity, CancellationToken cancellationToken = default)
    {
        await context.RequiredProblemLanguages.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RequiredProblemLanguage entity, CancellationToken cancellationToken = default)
    {
        context.RequiredProblemLanguages.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RequiredProblemLanguage?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.RequiredProblemLanguages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RequiredProblemLanguage>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default
    ) => await context.RequiredProblemLanguages.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RequiredProblemLanguage>> GetAllOrderedForTrackAsync(
        Guid trackId,
        CancellationToken cancellationToken = default
    )
    {
        List<Guid> trackLanguageVersionIds = await context
            .Languages.Where(l => l.TrackId == trackId)
            .SelectMany(l => l.Versions)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        return await context
            .RequiredProblemLanguages.Where(x => trackLanguageVersionIds.Contains(x.LanguageVersionId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<RequiredProblemLanguage?> FindByLanguageVersionIdAsync(
        Guid languageVersionId,
        CancellationToken cancellationToken = default
    ) =>
        await context.RequiredProblemLanguages.FirstOrDefaultAsync(
            x => x.LanguageVersionId == languageVersionId,
            cancellationToken
        );

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await context.RequiredProblemLanguages.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}