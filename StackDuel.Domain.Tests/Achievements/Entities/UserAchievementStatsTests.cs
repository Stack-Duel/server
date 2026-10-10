using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Achievements.Enums;
using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Domain.Tests.Achievements.Entities;

public class UserAchievementStatsTests
{
    private static readonly DateOnly Day1 = new(2026, 3, 1);

    private static UserAchievementStats CreateStats() => new(Guid.NewGuid());

    [Test]
    public void Constructor_SetsUserId()
    {
        Guid userId = Guid.NewGuid();
        Assert.That(new UserAchievementStats(userId).UserId, Is.EqualTo(userId));
    }

    [Test]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievementStats(Guid.Empty));
    }

    [Test]
    public void Constructor_StartsAllCountersAtZero()
    {
        UserAchievementStats stats = CreateStats();

        Assert.Multiple(() =>
        {
            Assert.That(stats.AcceptedSolveCount, Is.Zero);
            Assert.That(stats.DistinctDifficultiesSolved, Is.Zero);
            Assert.That(stats.DistinctLanguagesUsed, Is.Zero);
            Assert.That(stats.CurrentSolveStreak, Is.Zero);
            Assert.That(stats.LongestSolveStreak, Is.Zero);
            Assert.That(stats.LastSolveDateUtc, Is.Null);
            Assert.That(stats.GamesPlayed, Is.Zero);
            Assert.That(stats.BugReportsSubmitted, Is.Zero);
        });
    }

    [Test]
    public void RecordAcceptedSolve_IncrementsSolveCount()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(1));

        Assert.That(stats.AcceptedSolveCount, Is.EqualTo(2));
    }

    [Test]
    public void RecordAcceptedSolve_CountsEachDifficultyTierOnlyOnce()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Beginner, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Beginner, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Expert, Day1);

        Assert.That(stats.DistinctDifficultiesSolved, Is.EqualTo(2));
    }

    [Test]
    public void RecordAcceptedSolve_TracksEveryTierIndependently()
    {
        UserAchievementStats stats = CreateStats();

        foreach (DifficultyTier tier in Enum.GetValues<DifficultyTier>())
            stats.RecordAcceptedSolve(tier, Day1);

        Assert.That(stats.DistinctDifficultiesSolved, Is.EqualTo(Enum.GetValues<DifficultyTier>().Length));
    }

    [Test]
    public void RecordAcceptedSolve_FirstSolve_StartsStreakAtOne()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);

        Assert.Multiple(() =>
        {
            Assert.That(stats.CurrentSolveStreak, Is.EqualTo(1));
            Assert.That(stats.LongestSolveStreak, Is.EqualTo(1));
            Assert.That(stats.LastSolveDateUtc, Is.EqualTo(Day1));
        });
    }

    [Test]
    public void RecordAcceptedSolve_ConsecutiveDays_ExtendsStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(1));
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(2));

        Assert.Multiple(() =>
        {
            Assert.That(stats.CurrentSolveStreak, Is.EqualTo(3));
            Assert.That(stats.LongestSolveStreak, Is.EqualTo(3));
        });
    }

    [Test]
    public void RecordAcceptedSolve_GapInDays_ResetsStreakToOneButKeepsLongest()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(1));
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(5));

        Assert.Multiple(() =>
        {
            Assert.That(stats.CurrentSolveStreak, Is.EqualTo(1));
            Assert.That(stats.LongestSolveStreak, Is.EqualTo(2));
        });
    }

    [Test]
    public void RecordAcceptedSolve_SameDayAgain_DoesNotExtendStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);

        Assert.Multiple(() =>
        {
            Assert.That(stats.CurrentSolveStreak, Is.EqualTo(1));
            Assert.That(stats.AcceptedSolveCount, Is.EqualTo(2), "the solve still counts");
        });
    }

    [Test]
    public void RecordAcceptedSolve_BackdatedSolve_LeavesStreakAndLastSolveDateUntouched()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(3));
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);

        Assert.Multiple(() =>
        {
            Assert.That(stats.CurrentSolveStreak, Is.EqualTo(1));
            Assert.That(stats.LastSolveDateUtc, Is.EqualTo(Day1.AddDays(3)));
        });
    }

    [Test]
    public void RecordLanguageUsed_FirstTime_AddsLanguage()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordLanguageUsed(Guid.NewGuid());

        Assert.That(stats.DistinctLanguagesUsed, Is.EqualTo(1));
        Assert.That(stats.Languages, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecordLanguageUsed_SameLanguageTwice_IsIgnoredTheSecondTime()
    {
        UserAchievementStats stats = CreateStats();
        Guid languageId = Guid.NewGuid();

        stats.RecordLanguageUsed(languageId);
        stats.RecordLanguageUsed(languageId);

        Assert.That(stats.DistinctLanguagesUsed, Is.EqualTo(1));
    }

    [Test]
    public void RecordLanguageUsed_DifferentLanguages_AreCountedSeparately()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordLanguageUsed(Guid.NewGuid());
        stats.RecordLanguageUsed(Guid.NewGuid());

        Assert.That(stats.DistinctLanguagesUsed, Is.EqualTo(2));
    }

    [Test]
    public void RecordGameResult_Won_IncrementsWinsAndStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);

        Assert.Multiple(() =>
        {
            Assert.That(stats.GamesPlayed, Is.EqualTo(1));
            Assert.That(stats.GamesWon, Is.EqualTo(1));
            Assert.That(stats.CurrentWinStreak, Is.EqualTo(1));
            Assert.That(stats.LongestWinStreak, Is.EqualTo(1));
        });
    }

    [Test]
    public void RecordGameResult_Lost_IncrementsLossesAndBreaksWinStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Lost);

        Assert.Multiple(() =>
        {
            Assert.That(stats.GamesLost, Is.EqualTo(1));
            Assert.That(stats.CurrentWinStreak, Is.Zero);
            Assert.That(stats.LongestWinStreak, Is.EqualTo(2), "the best run so far is remembered");
        });
    }

    [Test]
    public void RecordGameResult_Drawn_CountsThePlayButLeavesTheWinStreakAlone()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Drawn);

        Assert.Multiple(() =>
        {
            Assert.That(stats.GamesPlayed, Is.EqualTo(2));
            Assert.That(stats.GamesDrawn, Is.EqualTo(1));
            Assert.That(stats.CurrentWinStreak, Is.EqualTo(1));
        });
    }

    [Test]
    public void RecordGameResult_LongestWinStreak_IsTheBestOfSeveralRuns()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Lost);
        stats.RecordGameResult(GameOutcome.Won);

        Assert.Multiple(() =>
        {
            Assert.That(stats.CurrentWinStreak, Is.EqualTo(1));
            Assert.That(stats.LongestWinStreak, Is.EqualTo(3));
        });
    }

    [Test]
    public void RecordBugReport_IncrementsCounter()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordBugReport();
        stats.RecordBugReport();

        Assert.That(stats.BugReportsSubmitted, Is.EqualTo(2));
    }

    [Test]
    public void GetStatValue_ReturnsTheMatchingCounterForEveryStat()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Beginner, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Expert, Day1.AddDays(1));
        stats.RecordLanguageUsed(Guid.NewGuid());
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Lost);
        stats.RecordBugReport();

        Assert.Multiple(() =>
        {
            Assert.That(stats.GetStatValue(AchievementStat.AcceptedSolveCount), Is.EqualTo(2));
            Assert.That(stats.GetStatValue(AchievementStat.DistinctDifficultiesSolved), Is.EqualTo(2));
            Assert.That(stats.GetStatValue(AchievementStat.DistinctLanguagesUsed), Is.EqualTo(1));
            Assert.That(stats.GetStatValue(AchievementStat.LongestSolveStreak), Is.EqualTo(2));
            Assert.That(stats.GetStatValue(AchievementStat.GamesPlayed), Is.EqualTo(2));
            Assert.That(stats.GetStatValue(AchievementStat.GamesWon), Is.EqualTo(1));
            Assert.That(stats.GetStatValue(AchievementStat.LongestWinStreak), Is.EqualTo(1));
            Assert.That(stats.GetStatValue(AchievementStat.BugReportsSubmitted), Is.EqualTo(1));
        });
    }

    [Test]
    public void GetStatValue_EveryDeclaredStatIsMapped()
    {
        UserAchievementStats stats = CreateStats();

        Assert.Multiple(() =>
        {
            foreach (AchievementStat stat in Enum.GetValues<AchievementStat>())
                Assert.That(() => stats.GetStatValue(stat), Throws.Nothing, $"{stat} has no mapping");
        });
    }

    [Test]
    public void GetStatValue_UnknownStat_Throws()
    {
        UserAchievementStats stats = CreateStats();

        Assert.Throws<ArgumentOutOfRangeException>(() => stats.GetStatValue((AchievementStat)999));
    }
}

public class UserAchievementStatLanguageTests
{
    [Test]
    public void Constructor_SetsLanguageId()
    {
        Guid languageId = Guid.NewGuid();
        Assert.That(new UserAchievementStatLanguage(languageId).LanguageId, Is.EqualTo(languageId));
    }

    [Test]
    public void Constructor_EmptyLanguageId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievementStatLanguage(Guid.Empty));
    }
}