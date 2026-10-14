using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

    public async Task<Game?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Games.Include(g => g.Participants).FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task SaveChangesAsync(Game entity, CancellationToken cancellationToken = default)
    {
        MarkUntrackedChildrenAsAdded(entity);

        await context.SaveChangesAsync(cancellationToken);
    }

    private void MarkUntrackedChildrenAsAdded(Game entity)
    {
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        IReadOnlyList<GameRoundResult> roundResults = [.. entity.Participants.SelectMany(p => p.RoundResults)];

        MarkDetachedAsAdded(entity.Participants);
        MarkDetachedAsAdded(entity.Rounds);
        MarkDetachedAsAdded(roundResults);
        MarkDetachedAsAdded(roundResults.Select(result => result.Solution).OfType<GameRoundSolution>());

        context.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private void MarkDetachedAsAdded<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        foreach (EntityEntry<TEntity> entry in entities.Select(context.Entry))
        {
            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}