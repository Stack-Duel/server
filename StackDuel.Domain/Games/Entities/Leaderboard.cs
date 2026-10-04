using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class Leaderboard : AggregateRoot
{
    public Leaderboard(Guid gameModeId, int timeLimitInSeconds)
    {
        if (gameModeId == Guid.Empty)
            throw new ArgumentException("Game mode id must not be empty.", nameof(gameModeId));
        if (timeLimitInSeconds <= 0)
            throw new ArgumentException("Time limit must be greater than zero.", nameof(timeLimitInSeconds));
        GameModeId = gameModeId;
        TimeLimitInSeconds = timeLimitInSeconds;
    }

    private Leaderboard() { }

    /// <summary>
    /// Records a completed run's score against this leaderboard, updating the participant's
    /// high score only if it improves on their existing one (or adding them as a new entry if
    /// they've never appeared here before).
    /// </summary>
    public void RecordScore(Guid userId, int score)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        LeaderboardParticipant? participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
        {
            _participants.Add(new LeaderboardParticipant(userId, score));
            return;
        }

        participant.UpdateHighScoreIfGreater(score);
    }

    public Guid GameModeId { get; private set; }

    public int TimeLimitInSeconds { get; private set; }

    public IReadOnlyCollection<LeaderboardParticipant> Participants => _participants.AsReadOnly();

    public IReadOnlyCollection<LeaderboardParticipant> Rankings =>
        [.. _participants.OrderByDescending(p => p.HighScore)];

    private readonly List<LeaderboardParticipant> _participants = [];
}