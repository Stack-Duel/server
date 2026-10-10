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

    [Test]
    public void CreateThreshold_SetsCatalogMetadata()
    {
        AchievementDefinition definition = CreateThreshold();

        Assert.Multiple(() =>
        {
            Assert.That(definition.Code, Is.EqualTo(new AchievementCode("ten-solves")));
            Assert.That(definition.Name, Is.EqualTo("Ten Solves"));
            Assert.That(definition.Description, Is.EqualTo("Solve ten problems."));
            Assert.That(definition.Category, Is.EqualTo(AchievementCategory.Solving));
            Assert.That(definition.Tier, Is.EqualTo(AchievementTier.Bronze));
            Assert.That(definition.IconKey, Is.EqualTo("icon-ten-solves"));
        });
    }

    [Test]
    public void CreateThreshold_SetsThresholdCriteriaAndLeavesCustomRuleKeyUnset()
    {
        AchievementDefinition definition = CreateThreshold(AchievementStat.GamesWon, 25);

        Assert.Multiple(() =>
        {
            Assert.That(definition.CriteriaType, Is.EqualTo(AchievementCriteriaType.Threshold));
            Assert.That(definition.CriteriaStat, Is.EqualTo(AchievementStat.GamesWon));
            Assert.That(definition.CriteriaThreshold, Is.EqualTo(25));
            Assert.That(definition.CustomRuleKey, Is.Null);
        });
    }

    [Test]
    public void CreateThreshold_IsActiveByDefault()
    {
        Assert.That(CreateThreshold().IsActive, Is.True);
    }

    [Test]
    public void CreateThreshold_SetsCreatedAt()
    {
        DateTime before = DateTime.UtcNow;

        AchievementDefinition definition = CreateThreshold();

        Assert.That(definition.CreatedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void CreateThreshold_HonoursIsSecret()
    {
        Assert.That(CreateThreshold(isSecret: true).IsSecret, Is.True);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateThreshold_NonPositiveThreshold_Throws(int threshold)
    {
        Assert.Throws<InvalidAchievementCriteriaException>(() => CreateThreshold(threshold: threshold));
    }

    [Test]
    public void CreateCustom_SetsCustomCriteriaAndLeavesThresholdFieldsUnset()
    {
        AchievementDefinition definition = CreateCustom();

        Assert.Multiple(() =>
        {
            Assert.That(definition.CriteriaType, Is.EqualTo(AchievementCriteriaType.Custom));
            Assert.That(definition.CustomRuleKey, Is.EqualTo("flawless-duel"));
            Assert.That(definition.CriteriaStat, Is.Null);
            Assert.That(definition.CriteriaThreshold, Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void CreateCustom_BlankRuleKey_Throws(string? customRuleKey)
    {
        Assert.Throws<InvalidAchievementCriteriaException>(() => CreateCustom(customRuleKey!));
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public void UpdateDetails_ReplacesNameDescriptionAndIcon()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.UpdateDetails("Renamed", "New description.", "icon-renamed");

        Assert.Multiple(() =>
        {
            Assert.That(definition.Name, Is.EqualTo("Renamed"));
            Assert.That(definition.Description, Is.EqualTo("New description."));
            Assert.That(definition.IconKey, Is.EqualTo("icon-renamed"));
        });
    }

    [Test]
    public void UpdateDetails_BlankName_ThrowsAndLeavesDetailsUntouched()
    {
        AchievementDefinition definition = CreateThreshold();

        Assert.Throws<ArgumentException>(() => definition.UpdateDetails("", "New description.", "icon-renamed"));
        Assert.That(definition.Name, Is.EqualTo("Ten Solves"));
    }

    [Test]
    public void UpdateThresholdCriteria_ReplacesStatAndThreshold()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.UpdateThresholdCriteria(AchievementStat.LongestWinStreak, 5);

        Assert.Multiple(() =>
        {
            Assert.That(definition.CriteriaStat, Is.EqualTo(AchievementStat.LongestWinStreak));
            Assert.That(definition.CriteriaThreshold, Is.EqualTo(5));
            Assert.That(definition.CriteriaType, Is.EqualTo(AchievementCriteriaType.Threshold));
        });
    }

    [Test]
    public void UpdateThresholdCriteria_ConvertsCustomDefinitionAndClearsRuleKey()
    {
        AchievementDefinition definition = CreateCustom();

        definition.UpdateThresholdCriteria(AchievementStat.GamesPlayed, 3);

        Assert.Multiple(() =>
        {
            Assert.That(definition.CriteriaType, Is.EqualTo(AchievementCriteriaType.Threshold));
            Assert.That(definition.CustomRuleKey, Is.Null);
        });
    }

    [Test]
    public void UpdateThresholdCriteria_NonPositiveThreshold_Throws()
    {
        AchievementDefinition definition = CreateThreshold();

        Assert.Throws<InvalidAchievementCriteriaException>(() =>
            definition.UpdateThresholdCriteria(AchievementStat.GamesWon, 0)
        );
    }

    [Test]
    public void SetActive_TogglesFlag()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.SetActive(false);
        Assert.That(definition.IsActive, Is.False);

        definition.SetActive(true);
        Assert.That(definition.IsActive, Is.True);
    }

    [Test]
    public void SetSecret_TogglesFlag()
    {
        AchievementDefinition definition = CreateThreshold();

        definition.SetSecret(true);
        Assert.That(definition.IsSecret, Is.True);

        definition.SetSecret(false);
        Assert.That(definition.IsSecret, Is.False);
    }

    [TestCase(9, false, Description = "below threshold")]
    [TestCase(10, true, Description = "exactly at threshold")]
    [TestCase(11, true, Description = "above threshold")]
    public void IsSatisfiedByThreshold_ComparesAgainstThresholdInclusively(int statValue, bool expected)
    {
        AchievementDefinition definition = CreateThreshold(threshold: 10);
        Assert.That(definition.IsSatisfiedByThreshold(statValue), Is.EqualTo(expected));
    }

    [Test]
    public void IsSatisfiedByThreshold_CustomCriteria_IsNeverSatisfied()
    {
        AchievementDefinition definition = CreateCustom();
        Assert.That(definition.IsSatisfiedByThreshold(int.MaxValue), Is.False);
    }
}