using StackDuel.Domain.Rankings.Entities;

namespace StackDuel.Domain.Rankings;

public interface IPlayerSeasonRatingWriteRepository
{
    Task<PlayerSeasonRating> FindAsync(
        Guid playerId, 
        Guid seasonId, 
        CancellationToken cancellationToken = default
        );

    Task AddAsync(
        PlayerSeasonRating playerSeasonRating,
        CancellationToken cancellationToken = default
        );
}