using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Achievements.Enums;
using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Domain.Tests.Achievements.Entities;

public class UserAchievementStatsTests
{
    private static readonly DateOnly Day1 = new(2026, 3, 1);

    private static UserAchievementStats CreateStats() => new(Guid.NewGuid());

    [Fact]
    public void Constructor_SetsUserId()
    {
        Guid userId = Guid.NewGuid();
        Assert.Equal(userId, new UserAchievementStats(userId).UserId);
    }

    [Fact]
    public void Constructor_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievementStats(Guid.Empty));
    }

    [Fact]
    public void Constructor_StartsAllCountersAtZero()
    {
        UserAchievementStats stats = CreateStats();

        Assert.Equal(0, stats.AcceptedSolveCount);
        Assert.Equal(0, stats.DistinctDifficultiesSolved);
        Assert.Equal(0, stats.DistinctLanguagesUsed);
        Assert.Equal(0, stats.CurrentSolveStreak);
        Assert.Equal(0, stats.LongestSolveStreak);
        Assert.Null(stats.LastSolveDateUtc);
        Assert.Equal(0, stats.GamesPlayed);
        Assert.Equal(0, stats.BugReportsSubmitted);
    }

    [Fact]
    public void RecordAcceptedSolve_IncrementsSolveCount()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(1));

        Assert.Equal(2, stats.AcceptedSolveCount);
    }

    [Fact]
    public void RecordAcceptedSolve_CountsEachDifficultyTierOnlyOnce()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Beginner, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Beginner, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Expert, Day1);

        Assert.Equal(2, stats.DistinctDifficultiesSolved);
    }

    [Fact]
    public void RecordAcceptedSolve_TracksEveryTierIndependently()
    {
        UserAchievementStats stats = CreateStats();

        foreach (DifficultyTier tier in Enum.GetValues<DifficultyTier>())
            stats.RecordAcceptedSolve(tier, Day1);

        Assert.Equal(Enum.GetValues<DifficultyTier>().Length, stats.DistinctDifficultiesSolved);
    }

    [Fact]
    public void RecordAcceptedSolve_FirstSolve_StartsStreakAtOne()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);

        Assert.Equal(1, stats.CurrentSolveStreak);
        Assert.Equal(1, stats.LongestSolveStreak);
        Assert.Equal(Day1, stats.LastSolveDateUtc);
    }

    [Fact]
    public void RecordAcceptedSolve_ConsecutiveDays_ExtendsStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(1));
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(2));

        Assert.Equal(3, stats.CurrentSolveStreak);
        Assert.Equal(3, stats.LongestSolveStreak);
    }

    [Fact]
    public void RecordAcceptedSolve_GapInDays_ResetsStreakToOneButKeepsLongest()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(1));
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(5));

        Assert.Equal(1, stats.CurrentSolveStreak);
        Assert.Equal(2, stats.LongestSolveStreak);
    }

    [Fact]
    public void RecordAcceptedSolve_SameDayAgain_DoesNotExtendStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);

        Assert.Equal(1, stats.CurrentSolveStreak);
        Assert.Equal(2, stats.AcceptedSolveCount); // the solve still counts
    }

    [Fact]
    public void RecordAcceptedSolve_BackdatedSolve_LeavesStreakAndLastSolveDateUntouched()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1.AddDays(3));
        stats.RecordAcceptedSolve(DifficultyTier.Easy, Day1);

        Assert.Equal(1, stats.CurrentSolveStreak);
        Assert.Equal(Day1.AddDays(3), stats.LastSolveDateUtc);
    }

    [Fact]
    public void RecordLanguageUsed_FirstTime_AddsLanguage()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordLanguageUsed(Guid.NewGuid());

        Assert.Equal(1, stats.DistinctLanguagesUsed);
        Assert.Single(stats.Languages);
    }

    [Fact]
    public void RecordLanguageUsed_SameLanguageTwice_IsIgnoredTheSecondTime()
    {
        UserAchievementStats stats = CreateStats();
        Guid languageId = Guid.NewGuid();

        stats.RecordLanguageUsed(languageId);
        stats.RecordLanguageUsed(languageId);

        Assert.Equal(1, stats.DistinctLanguagesUsed);
    }

    [Fact]
    public void RecordLanguageUsed_DifferentLanguages_AreCountedSeparately()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordLanguageUsed(Guid.NewGuid());
        stats.RecordLanguageUsed(Guid.NewGuid());

        Assert.Equal(2, stats.DistinctLanguagesUsed);
    }

    [Fact]
    public void RecordGameResult_Won_IncrementsWinsAndStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);

        Assert.Equal(1, stats.GamesPlayed);
        Assert.Equal(1, stats.GamesWon);
        Assert.Equal(1, stats.CurrentWinStreak);
        Assert.Equal(1, stats.LongestWinStreak);
    }

    [Fact]
    public void RecordGameResult_Lost_IncrementsLossesAndBreaksWinStreak()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Lost);

        Assert.Equal(1, stats.GamesLost);
        Assert.Equal(0, stats.CurrentWinStreak);
        Assert.Equal(2, stats.LongestWinStreak); // the best run so far is remembered
    }

    [Fact]
    public void RecordGameResult_Drawn_CountsThePlayButLeavesTheWinStreakAlone()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Drawn);

        Assert.Equal(2, stats.GamesPlayed);
        Assert.Equal(1, stats.GamesDrawn);
        Assert.Equal(1, stats.CurrentWinStreak);
    }

    [Fact]
    public void RecordGameResult_LongestWinStreak_IsTheBestOfSeveralRuns()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Lost);
        stats.RecordGameResult(GameOutcome.Won);

        Assert.Equal(1, stats.CurrentWinStreak);
        Assert.Equal(3, stats.LongestWinStreak);
    }

    [Fact]
    public void RecordBugReport_IncrementsCounter()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordBugReport();
        stats.RecordBugReport();

        Assert.Equal(2, stats.BugReportsSubmitted);
    }

    [Fact]
    public void GetStatValue_ReturnsTheMatchingCounterForEveryStat()
    {
        UserAchievementStats stats = CreateStats();

        stats.RecordAcceptedSolve(DifficultyTier.Beginner, Day1);
        stats.RecordAcceptedSolve(DifficultyTier.Expert, Day1.AddDays(1));
        stats.RecordLanguageUsed(Guid.NewGuid());
        stats.RecordGameResult(GameOutcome.Won);
        stats.RecordGameResult(GameOutcome.Lost);
        stats.RecordBugReport();

        Assert.Equal(2, stats.GetStatValue(AchievementStat.AcceptedSolveCount));
        Assert.Equal(2, stats.GetStatValue(AchievementStat.DistinctDifficultiesSolved));
        Assert.Equal(1, stats.GetStatValue(AchievementStat.DistinctLanguagesUsed));
        Assert.Equal(2, stats.GetStatValue(AchievementStat.LongestSolveStreak));
        Assert.Equal(2, stats.GetStatValue(AchievementStat.GamesPlayed));
        Assert.Equal(1, stats.GetStatValue(AchievementStat.GamesWon));
        Assert.Equal(1, stats.GetStatValue(AchievementStat.LongestWinStreak));
        Assert.Equal(1, stats.GetStatValue(AchievementStat.BugReportsSubmitted));
    }

    [Fact]
    public void GetStatValue_EveryDeclaredStatIsMapped()
    {
        UserAchievementStats stats = CreateStats();

        foreach (AchievementStat stat in Enum.GetValues<AchievementStat>())
            Assert.True(Record.Exception(() => stats.GetStatValue(stat)) is null, $"{stat} has no mapping");
    }

    [Fact]
    public void GetStatValue_UnknownStat_Throws()
    {
        UserAchievementStats stats = CreateStats();

        Assert.Throws<ArgumentOutOfRangeException>(() => stats.GetStatValue((AchievementStat)999));
    }
}

public class UserAchievementStatLanguageTests
{
    [Fact]
    public void Constructor_SetsLanguageId()
    {
        Guid languageId = Guid.NewGuid();
        Assert.Equal(languageId, new UserAchievementStatLanguage(languageId).LanguageId);
    }

    [Fact]
    public void Constructor_EmptyLanguageId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserAchievementStatLanguage(Guid.Empty));
    }
}