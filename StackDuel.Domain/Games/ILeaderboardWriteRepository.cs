using StackDuel.Domain.Games.Entities;

namespace StackDuel.Domain.Games;

public interface ILeaderboardWriteRepository
{
    Task AddAsync(Leaderboard entity, CancellationToken cancellationToken = default);
    Task<Leaderboard?> FindByGameModeAndTimeLimitAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        CancellationToken cancellationToken = default
    );
    Task SaveChangesAsync(Leaderboard entity, CancellationToken cancellationToken = default);
}