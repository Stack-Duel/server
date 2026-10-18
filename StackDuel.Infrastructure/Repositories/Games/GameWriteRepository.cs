using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Games;

internal sealed class GameWriteRepository(StackDuelDbContext context) : IGameWriteRepository
{
    public async Task AddAsync(Game entity, CancellationToken cancellationToken = default)
    {
        await context.Games.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}