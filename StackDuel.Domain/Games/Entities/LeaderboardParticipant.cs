using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class LeaderboardParticipant : Entity
{
    public LeaderboardParticipant(Guid userId, int highScore)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));
        if (highScore < 0)
            throw new ArgumentException("High score must not be negative.", nameof(highScore));
        UserId = userId;
        HighScore = highScore;
    }

    private LeaderboardParticipant() { }

    public Guid UserId { get; private set; }

    public int HighScore { get; private set; }

    internal void UpdateHighScoreIfGreater(int score)
    {
        if (score < 0)
            throw new ArgumentException("Score must not be negative.", nameof(score));

        if (score > HighScore)
            HighScore = score;
    }
}