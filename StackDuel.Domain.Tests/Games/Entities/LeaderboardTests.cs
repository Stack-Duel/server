using LeaderboardEntity = StackDuel.Domain.Games.Entities.Leaderboard;

namespace StackDuel.Domain.Tests.Games.Entities;

public class LeaderboardTests
{
    private static readonly Guid GameModeId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static LeaderboardEntity CreateLeaderboard() => new(GameModeId, timeLimitInSeconds: 300);

    [Test]
    public void RecordScore_AddsNewParticipant_WhenUserHasNoExistingEntry()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();

        leaderboard.RecordScore(UserId, 10);

        Assert.That(leaderboard.Rankings.Single().UserId, Is.EqualTo(UserId));
        Assert.That(leaderboard.Rankings.Single().HighScore, Is.EqualTo(10));
    }

    [Test]
    public void RecordScore_UpdatesHighScore_WhenNewScoreIsGreater()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();
        leaderboard.RecordScore(UserId, 10);

        leaderboard.RecordScore(UserId, 15);

        Assert.That(leaderboard.Rankings.Single().HighScore, Is.EqualTo(15));
    }

    [Test]
    public void RecordScore_KeepsExistingHighScore_WhenNewScoreIsLower()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();
        leaderboard.RecordScore(UserId, 15);

        leaderboard.RecordScore(UserId, 10);

        Assert.That(leaderboard.Rankings.Single().HighScore, Is.EqualTo(15));
    }

    [Test]
    public void RecordScore_DoesNotDuplicateParticipant_WhenCalledMultipleTimesForSameUser()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();

        leaderboard.RecordScore(UserId, 5);
        leaderboard.RecordScore(UserId, 20);

        Assert.That(leaderboard.Rankings, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecordScore_ThrowsArgumentException_WhenUserIdIsEmpty()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();

        Assert.Throws<ArgumentException>(() => leaderboard.RecordScore(Guid.Empty, 10));
    }

    [Test]
    public void Rankings_OrdersParticipantsByHighScoreDescending()
    {
        LeaderboardEntity leaderboard = CreateLeaderboard();
        Guid lowScorer = Guid.NewGuid();
        Guid highScorer = Guid.NewGuid();

        leaderboard.RecordScore(lowScorer, 5);
        leaderboard.RecordScore(highScorer, 25);

        Assert.That(leaderboard.Rankings.Select(p => p.UserId), Is.EqualTo(new[] { highScorer, lowScorer }));
    }

    [Test]
    public void Constructor_ThrowsArgumentException_WhenTimeLimitIsNotPositive()
    {
        Assert.Throws<ArgumentException>(() => new LeaderboardEntity(GameModeId, timeLimitInSeconds: 0));
    }
}