using StackDuel.Domain.Achievements.Enums;
using StackDuel.Domain.Achievements.Exceptions;
using StackDuel.Domain.Achievements.ValueObjects;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Achievements.Entities;

/// <summary>
/// A badge in the catalog. Admin-managed metadata, not an aggregate with a lifecycle of its
/// own — earning one is represented separately by <see cref="UserAchievement"/>.
/// </summary>
public sealed class AchievementDefinition : Entity
{
    private AchievementDefinition(
        AchievementCode code,
        string name,
        string description,
        AchievementCategory category,
        AchievementTier tier,
        string iconKey,
        bool isSecret,
        AchievementCriteriaType criteriaType,
        AchievementStat? criteriaStat,
        int? criteriaThreshold,
        string? customRuleKey
    )
    {
        Code = code;
        Category = category;
        Tier = tier;
        IsSecret = isSecret;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;

        SetDetails(name, description, iconKey);
        SetCriteria(criteriaType, criteriaStat, criteriaThreshold, customRuleKey);
    }

    private AchievementDefinition() { }

    public static AchievementDefinition CreateThreshold(
        AchievementCode code,
        string name,
        string description,
        AchievementCategory category,
        AchievementTier tier,
        string iconKey,
        bool isSecret,
        AchievementStat stat,
        int threshold
    )
    {
        return new AchievementDefinition(
            code,
            name,
            description,
            category,
            tier,
            iconKey,
            isSecret,
            AchievementCriteriaType.Threshold,
            stat,
            threshold,
            null
        );
    }

    public static AchievementDefinition CreateCustom(
        AchievementCode code,
        string name,
        string description,
        AchievementCategory category,
        AchievementTier tier,
        string iconKey,
        bool isSecret,
        string customRuleKey
    )
    {
        return new AchievementDefinition(
            code,
            name,
            description,
            category,
            tier,
            iconKey,
            isSecret,
            AchievementCriteriaType.Custom,
            null,
            null,
            customRuleKey
        );
    }

    public void UpdateDetails(string name, string description, string iconKey) =>
        SetDetails(name, description, iconKey);

    public void UpdateThresholdCriteria(AchievementStat stat, int threshold) =>
        SetCriteria(AchievementCriteriaType.Threshold, stat, threshold, null);

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetSecret(bool isSecret) => IsSecret = isSecret;

    /// <summary>Whether the given value of this definition's <see cref="CriteriaStat"/> satisfies it.</summary>
    public bool IsSatisfiedByThreshold(int currentStatValue) =>
        CriteriaType == AchievementCriteriaType.Threshold
        && CriteriaThreshold.HasValue
        && currentStatValue >= CriteriaThreshold.Value;

    private void SetDetails(string name, string description, string iconKey)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (string.IsNullOrWhiteSpace(iconKey))
            throw new ArgumentException("Icon key is required.", nameof(iconKey));

        Name = name;
        Description = description;
        IconKey = iconKey;
    }

    private void SetCriteria(
        AchievementCriteriaType criteriaType,
        AchievementStat? stat,
        int? threshold,
        string? customRuleKey
    )
    {
        if (criteriaType == AchievementCriteriaType.Threshold)
        {
            if (stat is null)
                throw new InvalidAchievementCriteriaException("A stat is required for threshold criteria.");

            if (threshold is not > 0)
                throw new InvalidAchievementCriteriaException("Threshold must be greater than zero.");
        }
        else if (string.IsNullOrWhiteSpace(customRuleKey))
        {
            throw new InvalidAchievementCriteriaException("A custom rule key is required for custom criteria.");
        }

        CriteriaType = criteriaType;
        CriteriaStat = stat;
        CriteriaThreshold = threshold;
        CustomRuleKey = customRuleKey;
    }

    public AchievementCode Code { get; private set; }

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public AchievementCategory Category { get; private set; }

    public AchievementTier Tier { get; private set; }

    public string IconKey { get; private set; } = null!;

    /// <summary>Hidden from the catalog/profile until earned.</summary>
    public bool IsSecret { get; private set; }

    public bool IsActive { get; private set; }

    public AchievementCriteriaType CriteriaType { get; private set; }

    public AchievementStat? CriteriaStat { get; private set; }

    public int? CriteriaThreshold { get; private set; }

    public string? CustomRuleKey { get; private set; }

    public DateTime CreatedAt { get; private set; }
}