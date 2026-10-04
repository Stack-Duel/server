using StackDuel.Domain.Achievements.Enums;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.SeedWork;
using System.Numerics;

namespace StackDuel.Domain.Achievements.Entities;

/// <summary>
/// Denormalized per-user counters that back achievement evaluation. Kept up to date incrementally
/// by domain event handlers reacting to submissions/games/feedback, so checking a threshold-type
/// <see cref="AchievementDefinition"/> is an O(1) comparison instead of re-aggregating history.
/// </summary>
public sealed class UserAchievementStats : Entity
{
    public UserAchievementStats(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        UserId = userId;
    }

    private UserAchievementStats() { }

    public void RecordAcceptedSolve(DifficultyTier difficulty, DateOnly solvedOnUtc)
    {
        AcceptedSolveCount++;

        _solvedDifficultyMask |= 1 << ((int)difficulty - 1);

        UpdateSolveStreak(solvedOnUtc);
    }

    public void RecordLanguageUsed(Guid languageId)
    {
        if (_languages.Any(l => l.LanguageId == languageId))
            return;

        _languages.Add(new UserAchievementStatLanguage(languageId));
    }

    public void RecordGameResult(GameOutcome outcome)
    {
        GamesPlayed++;

        switch (outcome)
        {
            case GameOutcome.Won:
                GamesWon++;
                CurrentWinStreak++;
                LongestWinStreak = Math.Max(LongestWinStreak, CurrentWinStreak);
                break;
            case GameOutcome.Lost:
                GamesLost++;
                CurrentWinStreak = 0;
                break;
            case GameOutcome.Drawn:
                GamesDrawn++;
                break;
        }
    }

    public void RecordBugReport() => BugReportsSubmitted++;

    /// <summary>Reads the value of a named stat, for the generic threshold evaluator.</summary>
    public int GetStatValue(AchievementStat stat) =>
        stat switch
        {
            AchievementStat.AcceptedSolveCount => AcceptedSolveCount,
            AchievementStat.DistinctDifficultiesSolved => DistinctDifficultiesSolved,
            AchievementStat.DistinctLanguagesUsed => DistinctLanguagesUsed,
            AchievementStat.LongestSolveStreak => LongestSolveStreak,
            AchievementStat.GamesPlayed => GamesPlayed,
            AchievementStat.GamesWon => GamesWon,
            AchievementStat.LongestWinStreak => LongestWinStreak,
            AchievementStat.BugReportsSubmitted => BugReportsSubmitted,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
        };

    private void UpdateSolveStreak(DateOnly solvedOnUtc)
    {
        if (LastSolveDateUtc is { } last)
        {
            if (solvedOnUtc <= last)
                return;

            CurrentSolveStreak = solvedOnUtc == last.AddDays(1) ? CurrentSolveStreak + 1 : 1;
        }
        else
        {
            CurrentSolveStreak = 1;
        }

        LastSolveDateUtc = solvedOnUtc;
        LongestSolveStreak = Math.Max(LongestSolveStreak, CurrentSolveStreak);
    }

    public Guid UserId { get; private set; }

    public int AcceptedSolveCount { get; private set; }

    public int DistinctDifficultiesSolved => BitOperations.PopCount((uint)_solvedDifficultyMask);

    public int DistinctLanguagesUsed => _languages.Count;

    public int CurrentSolveStreak { get; private set; }

    public int LongestSolveStreak { get; private set; }

    public DateOnly? LastSolveDateUtc { get; private set; }

    public int GamesPlayed { get; private set; }

    public int GamesWon { get; private set; }

    public int GamesLost { get; private set; }

    public int GamesDrawn { get; private set; }

    public int CurrentWinStreak { get; private set; }

    public int LongestWinStreak { get; private set; }

    public int BugReportsSubmitted { get; private set; }

    public IReadOnlyCollection<UserAchievementStatLanguage> Languages => _languages.AsReadOnly();

    private int _solvedDifficultyMask;

    private readonly List<UserAchievementStatLanguage> _languages = [];
}

public sealed class UserAchievementStatLanguage : Entity
{
    public UserAchievementStatLanguage(Guid languageId)
    {
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language id must not be empty.", nameof(languageId));

        LanguageId = languageId;
    }

    private UserAchievementStatLanguage() { }

    public Guid LanguageId { get; private set; }
}