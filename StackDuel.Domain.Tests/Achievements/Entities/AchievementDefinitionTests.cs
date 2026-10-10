using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Achievements.Enums;
using StackDuel.Domain.Achievements.Exceptions;
using StackDuel.Domain.Achievements.ValueObjects;

namespace StackDuel.Domain.Tests.Achievements.Entities;

public class AchievementDefinitionTests
{
    private static AchievementDefinition CreateThreshold(
        AchievementStat stat = AchievementStat.AcceptedSolveCount,
        int threshold = 10,
        bool isSecret = false
    ) =>
        AchievementDefinition.CreateThreshold(
            new AchievementCode("ten-solves"),
            "Ten Solves",
            "Solve ten problems.",
            AchievementCategory.Solving,
            AchievementTier.Bronze,
            "icon-ten-solves",
            isSecret,
            stat,
            threshold
        );

    private static AchievementDefinition CreateCustom(string customRuleKey = "flawless-duel") =>
        AchievementDefinition.CreateCustom(
            new AchievementCode("flawless"),
            "Flawless",
            "Win without a failed submission.",
            AchievementCategory.Dueling,
            AchievementTier.Gold,
            "icon-flawless",
            false,
            customRuleKey
        );

    [Fact]
    public void CreateThreshold_SetsCatalogMetadata()
    {
        AchievementDefinition definition = CreateThreshold();

        Assert.Equal(new AchievementCode("ten-solves"), definition.Code);
        Assert.Equal("Ten Solves", definition.Name);
        Assert.Equal("Solve ten problems.", definition.Description);
        Assert.Equal(AchievementCategory.Solving, definition.Category);
        Assert.Equal(AchievementTier.Bronze, definition.Tier);
        Assert.Equal("icon-ten-solves", definition.IconKey);
    }

    [Fact]
    public void CreateThreshold_SetsThresholdCriteriaAndLeavesCustomRuleKeyUnset()
    {
        AchievementDefinition definition = CreateThreshold(AchievementStat.GamesWon, 25);

        Assert.Equal(AchievementCriteriaType.Threshold, definition.CriteriaType);
        Assert.Equal(AchievementStat.GamesWon, definition.CriteriaStat);
        Assert.Equal(25, definition.CriteriaThreshold);
        Assert.Null(definition.CustomRuleKey);
    }

    [Fact]
    public void CreateThreshold_IsActiveByDefault()
    {
        Assert.True(CreateThreshold().IsActive);
    }

    [Fact]
    public void CreateThreshold_SetsCreatedAt()
    {
        DateTime before = DateTime.UtcNow;

        AchievementDefinition definition = CreateThreshold();

        Assert.True(definition.CreatedAt >= before);
    }

    [Fact]
    public void CreateThreshold_HonoursIsSecret()
    {
        Assert.True(CreateThreshold(isSecret: true).IsSecret);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateThreshold_NonPositiveThreshold_Throws(int threshold)
    {
        Assert.Throws<InvalidAchievementCriteriaException>(() => CreateThreshold(threshold: threshold));
    }

    [Fact]
    public void CreateCustom_SetsCustomCriteriaAndLeavesThresholdFieldsUnset()
    {
        AchievementDefinition definition = CreateCustom();

        Assert.Equal(AchievementCriteriaType.Custom, definition.CriteriaType);
        Assert.Equal("flawless-duel", definition.CustomRuleKey);
        Assert.Null(definition.CriteriaStat);
        Assert.Null(definition.CriteriaThreshold);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCustom_BlankRuleKey_Throws(string? customRuleKey)
    {
        Assert.Throws<InvalidAchievementCriteriaException>(() => CreateCustom(customRuleKey!));
    }

    [Fact]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AchievementDefinition.CreateThreshold(
                new AchievementCode("ten-solves"),
                "  ",
                "Solve ten problems.",
                AchievementCategory.Solving,
                AchievementTier.Bronze,
                "icon",
                false,
                AchievementStat.AcceptedSolveCount,
                10
            )
        );
    }

    [Fact]
    public void Create_BlankDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AchievementDefinition.CreateThreshold(
                new AchievementCode("ten-solves"),
                "Ten Solves",
                "  ",
                AchievementCategory.Solving,
                AchievementTier.Bronze,
                "icon",
                false,
                AchievementStat.AcceptedSolveCount,
                10
            )
        );
    }

    [Fact]
    public void Create_BlankIconKey_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AchievementDefinition.CreateThreshold(
                new AchievementCode("ten-solves"),
                "Ten Solves",
                "Solve ten problems.",
                AchievementCategory.Solving,
                AchievementTier.Bronze,
                "  ",
                false,
                AchievementStat.AcceptedSolveCount,
                10
            )
        );
    }

    [Fact]
    public void UpdateDetails_ReplacesNameDescriptionAndIcon()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.UpdateDetails("Renamed", "New description.", "icon-renamed");

        Assert.Equal("Renamed", definition.Name);
        Assert.Equal("New description.", definition.Description);
        Assert.Equal("icon-renamed", definition.IconKey);
    }

    [Fact]
    public void UpdateDetails_BlankName_ThrowsAndLeavesDetailsUntouched()
    {
        AchievementDefinition definition = CreateThreshold();

        Assert.Throws<ArgumentException>(() => definition.UpdateDetails("", "New description.", "icon-renamed"));
        Assert.Equal("Ten Solves", definition.Name);
    }

    [Fact]
    public void UpdateThresholdCriteria_ReplacesStatAndThreshold()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.UpdateThresholdCriteria(AchievementStat.LongestWinStreak, 5);

        Assert.Equal(AchievementStat.LongestWinStreak, definition.CriteriaStat);
        Assert.Equal(5, definition.CriteriaThreshold);
        Assert.Equal(AchievementCriteriaType.Threshold, definition.CriteriaType);
    }

    [Fact]
    public void UpdateThresholdCriteria_ConvertsCustomDefinitionAndClearsRuleKey()
    {
        AchievementDefinition definition = CreateCustom();

        definition.UpdateThresholdCriteria(AchievementStat.GamesPlayed, 3);

        Assert.Equal(AchievementCriteriaType.Threshold, definition.CriteriaType);
        Assert.Null(definition.CustomRuleKey);
    }

    [Fact]
    public void UpdateThresholdCriteria_NonPositiveThreshold_Throws()
    {
        AchievementDefinition definition = CreateThreshold();

        Assert.Throws<InvalidAchievementCriteriaException>(() =>
            definition.UpdateThresholdCriteria(AchievementStat.GamesWon, 0)
        );
    }

    [Fact]
    public void SetActive_TogglesFlag()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.SetActive(false);
        Assert.False(definition.IsActive);

        definition.SetActive(true);
        Assert.True(definition.IsActive);
    }

    [Fact]
    public void SetSecret_TogglesFlag()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.SetSecret(true);
        Assert.True(definition.IsSecret);

        definition.SetSecret(false);
        Assert.False(definition.IsSecret);
    }

    [Theory]
    [InlineData(9, false)] // below threshold
    [InlineData(10, true)] // exactly at threshold
    [InlineData(11, true)] // above threshold
    public void IsSatisfiedByThreshold_ComparesAgainstThresholdInclusively(int statValue, bool expected)
    {
        AchievementDefinition definition = CreateThreshold(threshold: 10);
        Assert.Equal(expected, definition.IsSatisfiedByThreshold(statValue));
    }

    [Fact]
    public void IsSatisfiedByThreshold_CustomCriteria_IsNeverSatisfied()
    {
        AchievementDefinition definition = CreateCustom();
        Assert.False(definition.IsSatisfiedByThreshold(int.MaxValue));
    }
}