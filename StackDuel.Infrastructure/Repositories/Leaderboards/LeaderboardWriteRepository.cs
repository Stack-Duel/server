using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Leaderboards;

internal sealed class LeaderboardWriteRepository(StackDuelDbContext context) : ILeaderboardWriteRepository
{
    public async Task AddAsync(Leaderboard entity, CancellationToken cancellationToken = default)
    {
        await context.Leaderboards.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Leaderboard?> FindByGameModeAndTimeLimitAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Leaderboards.Include(l => l.Participants)
            .FirstOrDefaultAsync(
                l => l.GameModeId == gameModeId && l.TimeLimitInSeconds == timeLimitInSeconds,
                cancellationToken
            );
    }

    public async Task SaveChangesAsync(Leaderboard entity, CancellationToken cancellationToken = default)
    {
        // See GameWriteRepository.SaveChangesAsync — LeaderboardParticipant.Id is likewise
        // assigned client-side, so a newly appended participant on an already-tracked
        // Leaderboard is indistinguishable from an existing one to EF's change detection
        // unless we force it to Added explicitly.
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        foreach (LeaderboardParticipant participant in entity.Participants)
        {
            EntityEntry<LeaderboardParticipant> entry = context.Entry(participant);
            if (entry.State == EntityState.Detached)
            {
                entry.State = EntityState.Added;
            }
        }
        context.ChangeTracker.AutoDetectChangesEnabled = true;

        await context.SaveChangesAsync(cancellationToken);
    }
}