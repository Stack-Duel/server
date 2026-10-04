namespace StackDuel.Domain.Achievements.Enums;

/// <summary>
/// Named counters on <see cref="Entities.UserAchievementStats"/> that a threshold-type
/// <see cref="Entities.AchievementDefinition"/> can be evaluated against.
/// </summary>
public enum AchievementStat
{
    AcceptedSolveCount,
    DistinctDifficultiesSolved,
    DistinctLanguagesUsed,
    LongestSolveStreak,
    GamesPlayed,
    GamesWon,
    LongestWinStreak,
    BugReportsSubmitted,
}