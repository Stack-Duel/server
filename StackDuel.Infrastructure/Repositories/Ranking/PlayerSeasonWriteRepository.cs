using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Rankings;
using StackDuel.Domain.Rankings.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Rankings;

internal sealed class PlayerSeasonRatingWriteRepository(
    StackDuelDbContext context
) : IPlayerSeasonRatingWriteRepository
{
    async Task<PlayerSeasonRating?>
        IPlayerSeasonRatingWriteRepository.FindAsync(
            Guid playerId,
            Guid seasonId,
            CancellationToken cancellationToken
        )
    {
        return await context.PlayerSeasonRatings
            .SingleOrDefaultAsync(
                rating =>
                    rating.PlayerId == playerId
                    && rating.SeasonId == seasonId,
                cancellationToken
            );
    }

    async Task IPlayerSeasonRatingWriteRepository.AddAsync(
        PlayerSeasonRating entity,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(entity);

        await context.PlayerSeasonRatings.AddAsync(
            entity,
            cancellationToken
        );
    }
}