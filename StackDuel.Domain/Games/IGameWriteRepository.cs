using StackDuel.Domain.Games.Entities;

namespace StackDuel.Domain.Games;

public interface IGameWriteRepository
{
    Task AddAsync(Game entity, CancellationToken cancellationToken = default);
}