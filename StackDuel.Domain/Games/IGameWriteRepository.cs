using StackDuel.Domain.Games.Entities;

namespace StackDuel.Domain.Games;

public interface IGameWriteRepository
{
    Task AddAsync(Game entity, CancellationToken cancellationToken = default);
    Task<Game?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(Game entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards any tracked, in-memory changes to this game and re-reads it from the store. Callers
    /// retrying after a lost write need the persisted state, not their own rejected mutations.
    /// </summary>
    Task<Game?> ReloadAsync(Guid gameId, CancellationToken cancellationToken = default);
}