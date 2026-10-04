namespace StackDuel.Domain.Achievements.Enums;

public enum AchievementCriteriaType
{
    /// <summary>A single named <see cref="AchievementStat"/> reaching a threshold value.</summary>
    Threshold,

    /// <summary>Evaluated by a code-registered rule (keyed by <see cref="Entities.AchievementDefinition.CustomRuleKey"/>) for conditions a single stat threshold can't express.</summary>
    Custom,
}