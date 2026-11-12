using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Rankings.Entities;

public sealed class PlayerSeasonRating : Entity
{
    public const int StartingRating = 1000;

    public PlayerSeasonRating(Guid playerId, Guid seasonId)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("Player ID must not be empty.", nameof(playerId));
        }

        if (seasonId == Guid.Empty)
        {
            throw new ArgumentException("Season ID must not be empty.", nameof(seasonId));
        }

        PlayerId = playerId;
        SeasonId = seasonId;
        Rating = StartingRating;
    }

    private PlayerSeasonRating()
    {
    }

    public Guid PlayerId { get; private set; }
    public Guid SeasonId { get; private set; }
    public int Rating { get; private set; }

    public int GamesPlayed { get; private set; }

    public void ApplyRatingChange(int ratingChange)
    {
        int nextRating = checked(Rating + ratingChange);
        int nextgamesPlayed = checked(GamesPlayed + 1);
        Rating = nextRating;
        GamesPlayed = nextgamesPlayed;
    }
}

