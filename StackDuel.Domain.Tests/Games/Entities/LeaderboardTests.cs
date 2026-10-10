using LeaderboardEntity = StackDuel.Domain.Games.Entities.Leaderboard;

namespace StackDuel.Domain.Tests.Games.Entities;

public class LeaderboardTests
{
    private static readonly Guid GameModeId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static LeaderboardEntity CreateLeaderboard() => new(GameModeId, timeLimitInSeconds: 300);

    [Fact]
    public void RecordScore_AddsNewParticipant_WhenUserHasNoExistingEntry()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();

        leaderboard.RecordScore(UserId, 10);

        Assert.Equal(UserId, leaderboard.Rankings.Single().UserId);
        Assert.Equal(10, leaderboard.Rankings.Single().HighScore);
    }

    [Fact]
    public void RecordScore_UpdatesHighScore_WhenNewScoreIsGreater()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();
        leaderboard.RecordScore(UserId, 10);

        leaderboard.RecordScore(UserId, 15);

        Assert.Equal(15, leaderboard.Rankings.Single().HighScore);
    }

    [Fact]
    public void RecordScore_KeepsExistingHighScore_WhenNewScoreIsLower()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();
        leaderboard.RecordScore(UserId, 15);

        leaderboard.RecordScore(UserId, 10);

        Assert.Equal(15, leaderboard.Rankings.Single().HighScore);
    }

    [Fact]
    public void RecordScore_DoesNotDuplicateParticipant_WhenCalledMultipleTimesForSameUser()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();

        leaderboard.RecordScore(UserId, 5);
        leaderboard.RecordScore(UserId, 20);

        Assert.Single(leaderboard.Rankings);
    }

    [Fact]
    public void RecordScore_ThrowsArgumentException_WhenUserIdIsEmpty()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();

        Assert.Throws<ArgumentException>(() => leaderboard.RecordScore(Guid.Empty, 10));
    }

    [Fact]
    public void Rankings_OrdersParticipantsByHighScoreDescending()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();
        Guid lowScorer = Guid.NewGuid();
        Guid highScorer = Guid.NewGuid();

        leaderboard.RecordScore(lowScorer, 5);
        leaderboard.RecordScore(highScorer, 25);

        Assert.Equal(new[] { highScorer, lowScorer }, leaderboard.Rankings.Select(p => p.UserId));
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenTimeLimitIsNotPositive()
    {
        Assert.Throws<ArgumentException>(() => new LeaderboardEntity(GameModeId, timeLimitInSeconds: 0));
    }
}