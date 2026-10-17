using Microsoft.EntityFrameworkCore;
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

}