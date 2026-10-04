using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Tracks;

public interface ITrackReadRepository
{
    Task<Track?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Track?> FindByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> FindByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Track>> GetActiveTracksAsync(CancellationToken cancellationToken = default);
}