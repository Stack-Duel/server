using StackDuel.Application.Tracks;
using StackDuel.Domain.Tracks.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Tracks;

internal sealed class TrackReadRepository(StackDuelDbContext context) : ITrackReadRepository
{
    public async Task<Track?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Tracks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Track?> FindByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        await context.Tracks.AsNoTracking().FirstOrDefaultAsync(t => t.Key == key && t.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Track>> FindByKeysAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default
    )
    {
        List<string> keyList = keys.ToList();

        return await context
            .Tracks.AsNoTracking()
            .Where(t => keyList.Contains(t.Key) && t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Track>> FindByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default
    )
    {
        List<Guid> idList = ids.ToList();

        return await context.Tracks.AsNoTracking().Where(t => idList.Contains(t.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Track>> GetActiveTracksAsync(CancellationToken cancellationToken = default) =>
        await context.Tracks.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(cancellationToken);
}